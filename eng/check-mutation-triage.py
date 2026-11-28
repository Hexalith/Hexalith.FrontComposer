"""Finite source/identity/status admission only; native scores/BOTH/private provenance remain mandatory."""
from pathlib import Path
import argparse,hashlib,json,re,sys

REL='tests/Hexalith.FrontComposer.SourceTools.Tests/Mutation/'
HISTORICAL_LEDGER=REL+'mutation-problem-followups.json'
CURRENT_LEDGER=REL+'mutation-current-identities.json'
CURRENT_SCHEMA='finite-source-tools-current-identities.v2'
SEGMENTS={'happy-path','error-handling'}
STATUSES={'Killed','Survived','NoCoverage','Timeout','CompileError','Ignored'}
PROBLEM={'Survived','NoCoverage','Timeout','CompileError'}
ROLE='Story10-4 SourceTools quality/release-automation role'
EXPRESSION_FIELDS = ('path', 'original', 'replacement', 'mutator')
REPORT_DIGEST_FIELDS = ('sha256', 'originalNativeSha256', 'privateBuildDigest',
                        'provenanceSha256', 'publicationCaptureSha256')

class AdmissionError(ValueError):pass

def require(condition,message):
    if not condition:raise AdmissionError(message)

def digest(data):return hashlib.sha256(data).hexdigest()

def canonical(text):return text.replace('\r\n','\n')

def offset(source,line,column):
    lines=source.splitlines(keepends=True)
    require(type(line) is int and type(column) is int and 1<=line<=len(lines) and column>=1,'Invalid mutation source span')
    width=(column-1)*2
    raw=lines[line-1].encode('utf-16-le')
    require(width<=len(raw),'Mutation column escapes source line')
    return sum(map(len,lines[:line-1]))+len(raw[:width].decode('utf-16-le'))

def extract(source,span):return source[offset(source,*span[:2]):offset(source,*span[2:])]

def location(span):return {'start':{'line':span[0],'column':span[1]},'end':{'line':span[2],'column':span[3]}}

def identity(row,sources):
    return {'id':row['id'],'path':row['path'],'sourceSha256':sources[row['path']]['sha256'],'location':location(row['span']),
            'original':row['original'],'replacement':row['replacement'],'mutator':row['mutator']}

def unique_object(pairs):
    result = {}
    for key, value in pairs:
        require(key not in result, 'Duplicate metadata key: ' + key)
        result[key] = value
    return result


def metadata_object(data):
    document = json.loads(data, object_pairs_hook=unique_object)
    require(isinstance(document, dict), 'Owner metadata must be an object')
    return document


def read_metadata(path):
    with path.open('rb') as stream:
        data = stream.read(1048576)
    require(len(data) < 1048576, 'Owner metadata exceeds existing 1 MiB limit')
    return metadata_object(data), digest(data)


def read_production_sources(owner, files):
    sources={}
    for path,expected in files.items():
        raw=(owner/path).read_bytes();require(digest(raw)==expected['sha256'],'Changed current production source: '+path)
        require(digest(canonical(raw.decode('utf-8-sig')).encode())==expected['canonicalLfSha256'],'Canonical source fingerprint drift')
        sources[path]=raw.decode('utf-8-sig')
    return sources


def load_metadata(owner):
    ledger, historical_digest = read_metadata(owner / HISTORICAL_LEDGER)
    manifest, _ = read_metadata(owner / (REL + 'mutation-target-manifest.json'))
    current_ref = manifest.get('currentIdentityLedger')
    if 'currentIdentityLedger' in manifest:
        require(isinstance(current_ref, dict) and current_ref.get('path') == CURRENT_LEDGER,
                'Invalid successor ledger reference')
        current, current_digest = read_metadata(owner / CURRENT_LEDGER)
        require(current_ref.get('sha256') == current_digest, 'Stale successor ledger-manifest binding')
        require(current.get('historicalLedgerSha256') == historical_digest, 'Changed historical ledger')
        historical, controls = validate_historical_ledger(owner, ledger, None)
        return load_current_metadata(owner, current, historical, controls, manifest, current_digest,
                                     ledger['evidenceCatalog']['sha256'])

    sources = read_production_sources(owner, ledger['sourceFiles'])
    rows, _ = validate_historical_ledger(owner, ledger, sources)
    validate_historical_aggregates(manifest, rows, historical_digest)
    return ledger, manifest, rows, sources


def mutation_identity_digest(row, source_files):
    return digest(json.dumps(identity(row, source_files), sort_keys=True, separators=(',', ':')).encode())


def validate_preserved_assertions(owner, control):
    for preserved in control['currentPreservation']:
        require(digest((owner / preserved['ownerFile']).read_bytes()) == preserved['currentFileSha256'],
                'Current preserved assertion source fingerprint changed')


def validate_historical_ledger(owner, ledger, sources):
    require(ledger['schemaVersion']=='finite-source-tools-technical-followups.v1','Unknown finite ledger schema')
    require(ledger['owner']==ROLE,'Unassigned finite follow-up role')
    require(ledger['originalIdentityCount']==828 and ledger['supplementalIdentityCount']==1 and len(ledger['mutations'])==829,'Finite original/supplemental identity inventory drift')
    rows={}
    for row in ledger['mutations']:
        ident=row['id'];require(ident not in rows,'Duplicate reviewed mutation identity')
        require(row['path'] in ledger['sourceFiles'],'Unknown reviewed source path')
        if sources is not None:
            require(extract(sources[row['path']],row['span'])==row['original'],'Changed reviewed exact source span: '+ident)
        require(mutation_identity_digest(row,ledger['sourceFiles'])==row['identitySha256'],'Reviewed mutation identity digest differs: '+ident)
        require(row['action'] in {'kill-test-added','equivalent-accepted','deferred-with-owner'},'Unsupported/blocking disposition')
        if 'supplementalReview' in row:
            require(ident=='12812' and row['observed']=={} and row['action']=='deferred-with-owner','Unreviewed supplemental identity')
            require(row['supplementalReview']['literalResult']=='survived-current94-case-control' and row['supplementalReview']['nativeOutcomeClaimed'] is False,'Fabricated supplemental native outcome')
            require(row['allowedRemaining']=={segment:['Survived','NoCoverage'] for segment in ['happy-path','error-handling']},'Unreviewed supplemental execution status')
        else:require(set(row['observed'])==set(row['allowedRemaining']),'Segment transition scope differs')
        if row['action']=='kill-test-added':require(not any(row['allowedRemaining'].values()),'Test-added metadata cannot admit a remaining problem')
        for segment,status in row['observed'].items():
            require(segment in {'happy-path','error-handling'} and status in {'Survived','NoCoverage','Timeout'},'Forged original observation')
            allowed=row['allowedRemaining'][segment]
            require(set(allowed)<=({status,'Survived'} if status=='NoCoverage' else {status}),'Unreviewed execution-status transition')
        if row['followup']:
            require(ledger['followups'][row['followup']]['owner']==ROLE and ident in ledger['followups'][row['followup']]['ids'],'Missing exact assigned follow-up')
        if row['action']=='deferred-with-owner':require(row['followup'] is not None,'Deferred gap has no finite follow-up')
        rows[ident]=row
    require(ledger['hosted12812Uncertainty']['hostedFinalJsonStatus'] is None and not ledger['hosted12812Uncertainty']['admittedAsProblem'],'Hosted12812 uncertainty fabricated')
    catalog_ref=ledger['evidenceCatalog']
    catalog_path=owner/catalog_ref['path']
    catalog, catalog_digest = read_metadata(catalog_path)
    require(catalog_digest==catalog_ref['sha256'],'Changed/missing finite control evidence catalog')
    require(catalog['schemaVersion']=='finite-regression-control-evidence.v1','Unknown evidence catalog schema')
    require(set(catalog['cases'])=={ident for ident,row in rows.items() if row['action']=='kill-test-added'},'Control evidence identity inventory differs')
    for ident,entry in catalog['cases'].items():
        require(rows[ident]['control']['catalogId']==ident and entry['identitySha256']==rows[ident]['identitySha256'],'Control evidence mutation identity differs')
        require(entry['executed']['commands'] and entry['executed']['testSources'] and entry['executed']['resultLocator'],'Control evidence is not replayable/resolvable')
        require(re.fullmatch(r'[0-9a-f]{64}',entry['executed']['resultSha256']) is not None,'Control result fingerprint missing')
        if sources is not None:
            validate_preserved_assertions(owner, entry)
    return rows, catalog['cases']


def validate_historical_aggregates(manifest, rows, historical_digest):
    for record in manifest['problemMutantTriage']:
        require(record['identityLedger']==HISTORICAL_LEDGER and record['identityLedgerSha256']==historical_digest,'Stale/changed ledger-manifest binding')
        ids=record['mutationIds'];require(type(record['count']) is int and record['count']==len(ids)==len(set(ids)),'Aggregate count is not its finite identity maximum')
        require(record['owner']==ROLE and bool(record['rationale']),'Missing real role/rationale')
        original=0;transition=0;supplemental=0
        for ident in ids:
            require(ident in rows,'Unknown identity in aggregate maximum')
            row=rows[ident];require(row['action']==record['action'],'Aggregate action mismatch')
            require(record['segment'] in row['allowedRemaining'],'Aggregate segment mismatch')
            status=row['observed'].get(record['segment'])
            if 'supplementalReview' in row:
                require(record['status'] in row['allowedRemaining'][record['segment']],'Unreviewed supplemental aggregate status')
                supplemental+=1
            elif status==record['status']:original+=1
            else:
                require(status=='NoCoverage' and record['status']=='Survived' and 'Survived' in row['allowedRemaining'][record['segment']],'Aggregate includes an unreviewed transition')
                transition+=1
        require(original==record['originalObservedCount'] and transition==record['explicitTransitionMaximum'] and supplemental==record['supplementalReviewedMaximum'],'Aggregate original/transition/supplemental maxima differ')
    expected_groups = {}
    for row in rows.values():
        for segment,statuses in row['allowedRemaining'].items():
            status=row['observed'].get(segment)
            if status is not None:expected_groups.setdefault((segment,status,row['action']),set()).add(row['id'])
            for allowed in statuses:
                if allowed != status:
                    expected_groups.setdefault((segment,allowed,row['action']),set()).add(row['id'])
    actual_groups = {}
    for record in manifest['problemMutantTriage']:
        key = (record['segment'],record['status'],record['action'])
        require(key not in actual_groups,'Duplicate aggregate disposition group')
        actual_groups[key] = set(record['mutationIds'])
    require(actual_groups == expected_groups,'Aggregate maxima do not exactly cover the finite reviewed identities/transitions')


def matches(value, pattern):
    return isinstance(value, str) and re.fullmatch(pattern, value) is not None


def has_text(value):
    return isinstance(value, str) and bool(value.strip())


def is_fingerprint(value, length=64):
    return matches(value, rf'[0-9a-f]{{{length}}}') and value != '0' * length


def retained_file(owner, locator, expected, retained):
    require(has_text(locator) and '\\' not in locator and '#' not in locator, 'Invalid retained evidence locator')
    path = Path(locator)
    require(not path.is_absolute() and '..' not in path.parts and path.as_posix() == locator
            and (owner / path).resolve().is_relative_to(owner.resolve()), 'Invalid retained evidence locator: ' + locator)
    require(is_fingerprint(expected), 'Invalid retained evidence digest: ' + locator)
    if path not in retained:
        target = owner / path
        require(not target.is_symlink(), 'Linked retained evidence: ' + locator)
        if target.exists():
            require(target.is_file(), 'Retained evidence is not a regular file: ' + locator)
            data = target.read_bytes()
            retained[path] = (digest(data), data)
        else:
            retained[path] = (None, None)
    actual, data = retained[path]
    require(actual is None or actual == expected, 'Retained evidence digest differs: ' + locator)
    return data


def validate_reviewed_reports(owner, ledger, retained):
    reports = ledger['reviewedReports']
    require(isinstance(reports, dict) and set(reports) == SEGMENTS, 'Successor ledger requires both reviewed reports')
    for segment, report in reports.items():
        require(isinstance(report, dict) and has_text(report.get('locator'))
                and all(is_fingerprint(report.get(field)) for field in REPORT_DIGEST_FIELDS),
                'Reviewed report requires all original, published and provenance digests: ' + segment)
        locator = report['locator']
        report_path = Path(locator)
        native_relative = f'reports/source-tools-{segment}.json'
        require(report_path.parts[-3:] == (segment, 'reports', f'source-tools-{segment}.json'),
                'Reviewed report locator belongs to a different segment: ' + segment)
        retained_file(owner, locator, report['sha256'], retained)
        segment_path = report_path.parent.parent
        data = retained_file(owner, (segment_path / 'tool-provenance.json').as_posix(),
                             report['provenanceSha256'], retained)
        if data is not None:
            provenance = metadata_object(data)
            require(provenance['source_commit'] == ledger['sourceCommit'] and provenance['segment'] == segment
                    and provenance['build']['build_digest'] == report['privateBuildDigest'],
                    'Reviewed report execution provenance differs: ' + segment)
        data = retained_file(owner, (segment_path / 'publication-digests.json').as_posix(),
                             report['publicationCaptureSha256'], retained)
        if data is not None:
            publication = metadata_object(data)
            entries = {item['path']: item for item in publication['files']}
            require(publication['segment'] == segment and len(entries) == len(publication['files'])
                    and entries.get(native_relative) == {'path': native_relative,
                                                         'publishedSha256': report['sha256'],
                                                         'originalSha256': report['originalNativeSha256']}
                    and entries.get('tool-provenance.json', {}).get('publishedSha256') == report['provenanceSha256'],
                    'Reviewed report publication provenance differs: ' + segment)
    return reports


def validate_current_control(owner, row, ledger, retained):
    control = row['control']
    require(isinstance(control, dict) and is_fingerprint(control.get('resultSha256'))
            and has_text(control.get('resultLocator')), 'Current control requires a retained result and digest')
    require(control.get('identitySha256') == row['identitySha256'], 'Current control does not bind the reviewed mutation')
    require(is_fingerprint(control.get('reviewReceiptSha256')) and has_text(control.get('reviewReceiptLocator')),
            'Current control requires a replay receipt and digest')
    result = Path(control['resultLocator'])
    receipt = Path(control['reviewReceiptLocator'])
    require(result.suffix == '.trx' and receipt.suffix == '.json' and result.parent == receipt.parent,
            'Current control result and replay receipt locators do not identify the same replay')
    test_sources = control.get('testSources')
    require(isinstance(test_sources, dict) and bool(test_sources), 'Current control requires bound test sources')
    for path, expected in test_sources.items():
        relative = Path(path)
        require(relative.is_relative_to('tests') and '..' not in relative.parts
                and (owner / relative).resolve().is_relative_to(owner.resolve() / 'tests'),
                'Current control test source is outside the test tree')
        require(digest((owner / relative).read_bytes()) == expected, 'Current control test source fingerprint changed')
    retained_file(owner, control['resultLocator'], control['resultSha256'], retained)
    data = retained_file(owner, control['reviewReceiptLocator'], control['reviewReceiptSha256'], retained)
    if data is not None:
        replay = metadata_object(data)
        case = replay.get('cases', {}).get(result.stem, {})
        literal = case.get('literal', {})
        outcome = case.get('test', {})
        files = replay['testFiles']
        require(replay['sourceCommit'] == ledger['sourceCommit']
                and replay['source'] == {'path': row['path'], 'sha256': ledger['sourceFiles'][row['path']]['sha256']}
                and all(literal.get(field) == row[field] for field in ('span', 'original', 'replacement'))
                and sorted(literal.get('historicalIds', [])) == sorted(row['historicalIds'])
                and len(files) == len(test_sources) and {file['path']: file['sha256'] for file in files} == test_sources
                and outcome.get('retainedTrx') == result.name and outcome.get('retainedTrxSha256') == control['resultSha256']
                and outcome.get('exitCode', 0) != 0 and bool(outcome.get('failedTestNames')),
                'Current replay receipt does not bind the mutation, test sources and result')


def validate_current_aggregates(manifest, rows, current_digest):
    expected = {}
    for ident, row in rows.items():
        for segment, statuses in row['allowedRemaining'].items():
            for status in statuses:
                expected.setdefault((segment, status, row['action']), set()).add(ident)

    actual = {}
    for record in manifest['problemMutantTriage']:
        require(record['identityLedger'] == CURRENT_LEDGER and record['identityLedgerSha256'] == current_digest,
                'Current aggregate is not bound to the successor ledger')
        ids = record['mutationIds']
        require(isinstance(ids, list) and type(record['count']) is int and record['count'] == len(ids) == len(set(ids)),
                'Current aggregate count differs from its unique identities')
        require(record['owner'] == ROLE and has_text(record['rationale']), 'Current aggregate requires an owner and rationale')
        group = (record['segment'], record['status'], record['action'])
        require(group not in actual, 'Duplicate current aggregate disposition group')
        actual[group] = set(ids)
    require(actual == expected, 'Current aggregate maxima do not exactly cover reviewed statuses')


def validate_reviewed_crosswalk(owner, ledger, reports, retained):
    reference = ledger['reviewedCrosswalk']
    require(isinstance(reference, dict), 'Expected a reviewed crosswalk reference')
    data = retained_file(owner, reference.get('locator'), reference.get('sha256'), retained)
    if data is None:
        return None
    crosswalk = metadata_object(data)
    require(all(crosswalk[field] == ledger[field] for field in
                ('sourceCommit', 'historicalLedgerSha256', 'historicalLedgerMetadataCommit')),
            'Reviewed crosswalk source or historical ledger differs')
    original_field = {'originalNativeSha256': 'nativeOriginalSha256'}
    require(set(crosswalk['reviewedReportInputs']) == SEGMENTS
            and all(crosswalk['reviewedReportInputs'][segment][original_field.get(field, field)] == report[field]
                    for segment, report in reports.items()
                    for field in ('locator', 'sha256', 'originalNativeSha256', 'privateBuildDigest', 'provenanceSha256')),
            'Reviewed crosswalk report provenance differs')
    return crosswalk


def expression(row):
    return {field: row[field] for field in (*EXPRESSION_FIELDS, 'span')}


def validate_retirement(ident, record, old, ledger, reports, crosswalk):
    require(isinstance(record, dict) and has_text(record.get('reason')),
            'Historical retirement requires a reason and evidence')
    reference = ledger['reviewedCrosswalk']
    require(record.get('crosswalkSha256') == reference['sha256']
            and record.get('evidence') == f"{reference['locator']}#/retirementEvidence/{ident}",
            'Historical retirement crosswalk binding differs: ' + ident)
    observed = record.get('historicalObservation')
    require(isinstance(observed, dict) and observed.get('historicalId') == ident and observed.get('path') == old['path']
            and all(observed.get('ledger' + field.capitalize()) == old[field]
                    for field in ('span', 'original', 'replacement', 'mutator'))
            and observed.get('segment') in old['observed'] and observed.get('status') == old['observed'][observed['segment']]
            and record.get('historicalIdentitySha256') == old['identitySha256'],
            'Historical retirement does not bind the exact historical row: ' + ident)
    require(record.get('otherCurrentIdentityTried') == expression(old)
            and record.get('currentSourceFileSha256') == ledger['sourceFiles'][old['path']]['sha256']
            and record.get('bothCurrentReportSha256') == {segment: report['sha256'] for segment, report in reports.items()},
            'Historical retirement current source or reviewed report binding differs: ' + ident)
    if crosswalk is not None:
        evidence = crosswalk['retirementEvidence'][ident]
        require(all(evidence[field] == record[field] for field in
                    ('historicalObservation', 'otherCurrentIdentityTried', 'currentSourceFileSha256', 'bothCurrentReportSha256')),
                'Historical retirement differs from the retained crosswalk: ' + ident)


def validate_span_correction(record, old, row, reference, crosswalk):
    ident = old['id']
    expected = {
        'historicalIdentitySha256': old['identitySha256'], 'historicalSpan': old['span'],
        'identitySha256': row['identitySha256'], 'currentSpan': row['span'], 'crosswalkSha256': reference['sha256'],
    }
    require(record == expected, 'Current span correction requires an exact reviewed mapping: ' + ident)
    if crosswalk is not None:
        matches = [item for item in crosswalk['interveningLegacyBindingsThatDifferFromExactContext']
                   if item['historicalId'] == ident]
        require(len(matches) == 1 and matches[0]['interveningLegacyBinding'] == expression(old)
                and matches[0]['currentIdentity'] == expression(row)
                and all(any(candidate['id'] == row['id'] and candidate['compilerCandidate'] is False
                            for candidate in matches[0]['nativeBySegment'].get(segment, []))
                        for segment in row['allowedRemaining']),
                'Current span correction differs from the retained crosswalk: ' + ident)


def load_current_metadata(owner, ledger, historical, historical_controls, manifest, current_digest, historical_catalog_digest):
    require(ledger['schemaVersion'] == CURRENT_SCHEMA and ledger['owner'] == ROLE, 'Unknown or unowned successor ledger')
    require(is_fingerprint(ledger.get('sourceCommit'), 40), 'Successor ledger requires an exact reviewed source commit')
    require(is_fingerprint(ledger.get('historicalLedgerMetadataCommit'), 40),
            'Successor ledger requires an exact historical metadata commit')
    require(ledger.get('historicalControlCatalogSha256') == historical_catalog_digest,
            'Successor ledger does not bind the authenticated historical control catalog')
    retained = {}
    reports = validate_reviewed_reports(owner, ledger, retained)
    crosswalk = validate_reviewed_crosswalk(owner, ledger, reports, retained)

    source_files = ledger['sourceFiles']
    require(isinstance(source_files, dict) and bool(source_files), 'Successor ledger requires exact source fingerprints')
    for path in source_files:
        relative = Path(path)
        resolved_path = (owner / relative).resolve()
        require(any(relative.is_relative_to(root) and resolved_path.is_relative_to((owner / root).resolve())
                    for root in manifest['approvedTargetRoots'])
                and '..' not in relative.parts and relative.suffix == '.cs' and resolved_path.is_relative_to(owner.resolve()),
                'Successor source is outside the approved target roots')
    sources = read_production_sources(owner, source_files)

    retired = ledger['retirements']
    require(isinstance(retired, dict) and set(retired) <= set(historical), 'Unknown retired historical identity')
    for ident, record in retired.items():
        validate_retirement(ident, record, historical[ident], ledger, reports, crosswalk)
    corrections = ledger['spanCorrections']
    require(isinstance(corrections, dict), 'Span corrections must be exact historical mappings')
    used_corrections = set()
    resolved = set(retired)
    rows = {}
    require(isinstance(ledger['mutations'], list) and bool(ledger['mutations']), 'Successor ledger requires reviewed mutations')
    for row in ledger['mutations']:
        require(isinstance(row, dict), 'Current mutation must be an object')
        ident = row['id']
        require(matches(ident, r'0|[1-9][0-9]*') and ident not in rows, 'Invalid or duplicate current mutation ID')
        span = row['span']
        require(isinstance(span, list) and len(span) == 4 and all(type(point) is int for point in span)
                and span[:2] < span[2:], 'Invalid current mutation span')
        require(row['path'] in sources and extract(sources[row['path']], span) == row['original'],
                'Changed reviewed exact source span: ' + ident)
        require(has_text(row['mutator']) and isinstance(row['replacement'], str), 'Current mutation lacks an exact replacement and mutator')
        require(mutation_identity_digest(row, source_files) == row['identitySha256'], 'Current mutation identity digest differs: ' + ident)
        require(row['action'] in {'kill-test-added', 'equivalent-accepted', 'deferred-with-owner'}
                and has_text(row['rationale']), 'Current mutation requires a reviewed disposition')
        allowed = row['allowedRemaining']
        require(isinstance(allowed, dict) and bool(allowed) and set(allowed) <= SEGMENTS, 'Unknown current mutation segment')
        for statuses in allowed.values():
            require(isinstance(statuses, list) and len(statuses) == len(set(statuses))
                    and set(statuses) <= PROBLEM - {'CompileError'}, 'Invalid current mutation status allowance')
        if row['action'] == 'kill-test-added':
            require(not any(allowed.values()), 'Test-added metadata cannot admit a remaining problem')
        if row['action'] == 'deferred-with-owner':
            followup = row.get('followup')
            require(isinstance(followup, dict) and followup.get('owner') == ROLE and has_text(followup.get('action')),
                    'Current deferred mutation requires an assigned follow-up')

        previous_ids = row['historicalIds']
        require(isinstance(previous_ids, list) and len(previous_ids) == len(set(previous_ids))
                and set(previous_ids) <= set(historical) - resolved, 'Unknown or multiply resolved historical identity')
        previous = [historical[previous_id] for previous_id in previous_ids]
        resolved.update(previous_ids)
        changed_expression = False
        for old in previous:
            old_allowed = old['allowedRemaining']
            require(row['action'] == old['action'] and set(allowed) == set(old_allowed)
                    and all(set(statuses) <= set(old_allowed[segment]) for segment, statuses in allowed.items()),
                    'Current binding changed a historical action, segment or status allowance')
            same_expression = all(row[field] == old[field] for field in EXPRESSION_FIELDS)
            require(same_expression or row['action'] == 'kill-test-added',
                    'Current allowance changed a historical expression: ' + old['id'])
            if same_expression and row['span'] != old['span']:
                validate_span_correction(corrections.get(old['id']), old, row, ledger['reviewedCrosswalk'], crosswalk)
                used_corrections.add(old['id'])
            changed_expression |= not same_expression
        if 'control' in row:
            validate_current_control(owner, row, ledger, retained)
        elif row['action'] == 'kill-test-added':
            require(len(previous) == 1 and not changed_expression, 'Current control requires a retained result and digest')
            validate_preserved_assertions(owner, historical_controls[previous_ids[0]])
        rows[ident] = row

    require(resolved == set(historical), 'Successor ledger does not account for every historical identity')
    require(used_corrections == set(corrections), 'Unexpected span correction without a matching historical binding')
    validate_current_aggregates(manifest, rows, current_digest)
    return ledger, manifest, rows, sources

def report_path(value):
    value=value.replace('\\','/');marker='src/Hexalith.FrontComposer.SourceTools/'
    require(marker in value,'Problem source path is outside SourceTools')
    relative=marker+value.split(marker,1)[1]
    require('..' not in Path(relative).parts,'Problem source path traverses repository')
    return relative

def qualified_invalid_candidate(mutant):
    # This classification is conditional on the unchanged strict validator's actual
    # private-runtime qualification. It is not standalone compiler/tool admission.
    return (mutant.get('status')=='CompileError' and mutant.get('statusReason')=='Mutant caused compile errors'
            and re.fullmatch(r'[0-9]+',str(mutant.get('id',''))) is not None
            and mutant.get('coveredBy')==[] and mutant.get('killedBy')==[])

def inspect_report(report,segment,rows,sources):
    require(segment in SEGMENTS,'Unknown segment')
    require(isinstance(report, dict) and isinstance(report.get('files'), dict), 'Native report requires a file inventory')
    failures=[];reviewed=[];invalid=0;seen=set()
    for file_path,file in report['files'].items():
        require(isinstance(file, dict) and isinstance(file.get('mutants'), list), 'Native report file requires a mutation inventory')
        path=None;matches_current_source=None
        for mutant in file['mutants']:
            require(isinstance(mutant, dict), 'Native report mutation must be an object')
            ident=str(mutant.get('id',''));status=mutant.get('status')
            require(ident not in seen,'Duplicate actual mutation ID: '+ident);seen.add(ident)
            if status not in STATUSES:
                failures.append({'id':ident,'reason':'Unknown/unhandled actual status'});continue
            if ident not in rows and qualified_invalid_candidate(mutant):invalid+=1;continue
            is_problem=status in PROBLEM
            if ident not in rows:
                if is_problem:failures.append({'id':ident,'reason':'Unknown/unreviewed fresh problem identity'})
                continue
            row=rows[ident]
            if path is None:
                path=report_path(file_path)
                matches_current_source=path in sources and canonical(file['source'])==canonical(sources[path])
            try:
                require(path==row['path'],'Changed source path')
                require(mutant['location']==location(row['span']),'Changed source span')
                require(mutant['replacement']==row['replacement'],'Changed replacement')
                require(mutant.get('mutatorName')==row['mutator'],'Changed mutator identity')
                require(matches_current_source,'Actual report source differs from current production')
                # Exact source spans were validated by load_metadata; matching whole source preserves them.
                require(status!='Ignored','Reviewed live identity cannot become an unapproved exclusion/Ignored status')
                require(status!='CompileError','Reviewed live identity cannot be resolved by an unapproved compiler-candidate transition')
                if is_problem:require(status in row['allowedRemaining'].get(segment,[]),'Remaining problem/status is not admitted; tested kill requires actual fresh resolution')
            except (AdmissionError,KeyError) as error:
                failures.append({'id':ident,'reason':str(error)});continue
            if is_problem:reviewed.append({'id':ident,'status':status,'action':row['action'],'identitySha256':row['identitySha256']})
    expected={ident for ident,row in rows.items() if segment in row['allowedRemaining']}
    for ident in sorted(expected-seen,key=int):failures.append({'id':ident,'reason':'Expected reviewed identity missing from report'})
    return {'scope':__doc__,'segment':segment,'expectedReviewedIdentities':len(expected),'seenReviewedIdentities':len(expected & seen),'remainingProblemsAdmitted':reviewed,'conditionalInvalidCompilerCandidates':invalid,'failures':failures,
            'acceptedByThisFiniteCheck':not failures,'nativeScoresBothCohortsPrivateProvenanceStillRequired':True}

def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.parse_args()
    owner=Path(__file__).resolve().parents[1]
    output=owner/'artifacts/mutation/mutation-triage-admission.json'
    result={'scope':__doc__,'acceptedByThisFiniteCheck':False,'segments':[],
            'nativeScoresBothCohortsPrivateProvenanceStillRequired':True}
    try:
        ledger,manifest,rows,sources=load_metadata(owner)
        if 'currentIdentityLedger' in manifest:
            result['finiteLedgerSha256'] = manifest['currentIdentityLedger']['sha256']
            result['historicalLedgerSha256'] = ledger['historicalLedgerSha256']
        else:
            result['finiteLedgerSha256'] = digest((owner / HISTORICAL_LEDGER).read_bytes())
        for segment in ['happy-path','error-handling']:
            try:
                directory=owner/'artifacts/mutation'/segment
                candidates=sorted(directory.glob('**/reports/source-tools-'+segment+'.json'))
                require(len(candidates)==1,'Missing or ambiguous exact JSON report for '+segment)
                report=candidates[0]
                require(report.is_file() and not report.is_symlink(),'Report is not an unlinked regular JSON file')
                require(report.resolve().is_relative_to(directory.resolve()),'Report escapes its fixed segment directory')
                report_bytes = report.read_bytes()
                checked=inspect_report(json.loads(report_bytes),segment,rows,sources)
                checked['actualReportSha256']=digest(report_bytes)
                checked['report']=report.relative_to(owner).as_posix()
            except (AdmissionError,OSError,KeyError,ValueError,TypeError) as error:
                checked={'segment':segment,'acceptedByThisFiniteCheck':False,'failures':[], 'error':str(error)}
            result['segments'].append(checked)
        result['acceptedByThisFiniteCheck']=all(segment['acceptedByThisFiniteCheck'] for segment in result['segments'])
        code=0 if result['acceptedByThisFiniteCheck'] else 1
    except (AdmissionError,OSError,KeyError,ValueError,TypeError) as error:
        result['error']=str(error);code=2
    output.parent.mkdir(parents=True,exist_ok=True)
    output.write_text(json.dumps(result,indent=2)+'\n')
    print(json.dumps({'acceptedByThisFiniteCheck':result['acceptedByThisFiniteCheck'],
                     'failureCount':sum(len(segment.get('failures',[])) for segment in result['segments']),
                     'error':result.get('error')}))
    return code

if __name__=='__main__':raise SystemExit(main())
