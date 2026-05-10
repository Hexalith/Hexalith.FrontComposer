#!/usr/bin/env python3
"""Edge case audit: compare stubs vs registry."""
import json
import re
import os
import sys

ROOT = r'D:/Hexalith.FrontComposer'
DOCS = os.path.join(ROOT, 'docs', 'diagnostics')

def parse_front_matter(path):
    with open(path, 'r', encoding='utf-8') as f:
        text = f.read()
    fm = {}
    body = text
    if text.startswith('---\n') or text.startswith('---\r\n'):
        # find closing
        m = re.match(r'^---\r?\n(.*?)\r?\n---\r?\n(.*)', text, re.DOTALL)
        if m:
            yaml_block = m.group(1)
            body = m.group(2)
            for line in yaml_block.split('\n'):
                line = line.rstrip('\r')
                if not line.strip():
                    continue
                if line.startswith(' ') or line.startswith('\t'):
                    # nested - skip for now
                    continue
                if ':' in line:
                    k, v = line.split(':', 1)
                    fm[k.strip()] = v.strip()
    return fm, body, text

with open(os.path.join(DOCS, 'diagnostic-registry.json'), encoding='utf-8') as f:
    reg = json.load(f)

diags = {d['id']: d for d in reg['diagnostics']}

stub_files = sorted([f for f in os.listdir(DOCS) if f.startswith('HFC') and f.endswith('.md')])
print(f'Stubs found: {len(stub_files)}')
print(f'Registry entries: {len(diags)}')

# Coverage gap: stub-not-in-registry
stub_ids = {f[:-3] for f in stub_files}
reg_ids = set(diags.keys())
print(f'\n=== Coverage gaps ===')
print(f'In stubs not registry: {sorted(stub_ids - reg_ids)}')
print(f'In registry not stubs: {sorted(reg_ids - stub_ids)}')

# Drift table
drifts = []
canonical_help_link_format = reg['canonicalHelpLinkFormat']

for fname in stub_files:
    hfc_id = fname[:-3]
    path = os.path.join(DOCS, fname)
    fm, body, raw = parse_front_matter(path)
    if hfc_id not in diags:
        continue
    d = diags[hfc_id]

    # field comparisons
    expected = {
        'id': hfc_id,
        'title': d.get('title', ''),
        'ownerPackage': d.get('ownerPackage', ''),
        'severity': None,  # tricky
        'lifecycle': d.get('lifecycle', ''),
        'introducedIn': d.get('introducedIn', ''),
        'docsSlug': d.get('docsSlug', ''),
        'storyOwner': d.get('ownerStory', ''),
    }

    for k, v in expected.items():
        if v is None:
            continue
        actual = fm.get(k, '<MISSING>')
        # strip surrounding quotes
        a = actual.strip('"').strip("'")
        e = str(v).strip('"').strip("'")
        if a != e:
            drifts.append((hfc_id, k, e, actual))

    # help link cross-check
    expected_help = canonical_help_link_format.format(hfc_id)
    if expected_help not in raw:
        drifts.append((hfc_id, 'help-link-presence', expected_help, '<MISSING-IN-BODY>'))

    # check for capitalized vs lowercase id in body links
    lower_id = hfc_id.lower()
    if f'/{lower_id}' in raw and f'/{hfc_id}' not in raw:
        drifts.append((hfc_id, 'help-link-case', f'/{hfc_id}', f'/{lower_id}'))

    # docsSlug parity
    if 'docsSlug' in fm:
        slug = fm['docsSlug'].strip('"').strip("'")
        if slug != f'diagnostics/{hfc_id}':
            drifts.append((hfc_id, 'docsSlug-format', f'diagnostics/{hfc_id}', slug))

    # lifecycle/severity sanity
    lifecycle = fm.get('lifecycle', '').strip('"').strip("'")
    severity = fm.get('severity', '').strip('"').strip("'")
    reg_lifecycle = d.get('lifecycle', '')
    if reg_lifecycle == 'reserved' and severity not in ('', 'reserved', 'none', 'null'):
        drifts.append((hfc_id, 'reserved-has-severity', 'empty/reserved', severity))

    if reg_lifecycle == 'deprecated':
        if 'deprecatedIn' not in fm:
            drifts.append((hfc_id, 'deprecated-missing-deprecatedIn-frontmatter', 'present', 'absent'))
        if 'removedIn' not in fm and d.get('removedIn'):
            drifts.append((hfc_id, 'deprecated-missing-removedIn-frontmatter', 'present', 'absent'))

print(f'\n=== Total drifts: {len(drifts)} ===')
# Group by type
by_type = {}
for hfc, k, exp, act in drifts:
    by_type.setdefault(k, []).append((hfc, exp, act))

for t, items in sorted(by_type.items()):
    print(f'\n--- {t} ({len(items)} entries) ---')
    for hfc, exp, act in items[:30]:
        print(f'  {hfc}: expected={exp!r} actual={act!r}')
    if len(items) > 30:
        print(f'  ... +{len(items)-30} more')
