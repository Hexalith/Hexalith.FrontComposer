import manifestJson from '../specimens/frontcomposer-specimen-manifest.json' with { type: 'json' };

export const SPECIMEN_OWNER = '10-2-accessibility-ci-gates-and-visual-specimen-verification';

export interface SpecimenRoute {
  name: 'type' | 'data-formatting';
  path: string;
  readySelector: string;
  landmarkRoot: string;
  requiredSections: string[];
  expectedArtifacts: string[];
}

export interface ThemeDensityCombination {
  theme: 'light' | 'dark';
  density: 'compact' | 'comfortable' | 'roomy';
  artifact: string;
}

export interface SpecimenManifest {
  ownerStory: string;
  routes: SpecimenRoute[];
  themeDensityCombinations: ThemeDensityCombination[];
}

export const specimenManifest = manifestJson as SpecimenManifest;

export const validateSpecimenManifest = (manifest: SpecimenManifest = specimenManifest): void => {
  if (manifest.ownerStory !== SPECIMEN_OWNER) {
    throw new Error(`Specimen manifest owner must be ${SPECIMEN_OWNER}.`);
  }

  const paths = new Set<string>();
  const names = new Set<string>();
  for (const route of manifest.routes) {
    if (!route.path.startsWith('/__frontcomposer/specimens/')) {
      throw new Error(`${SPECIMEN_OWNER}: specimen route ${route.name} has a stale path ${route.path}.`);
    }

    if (paths.has(route.path) || names.has(route.name)) {
      throw new Error(`${SPECIMEN_OWNER}: duplicate specimen manifest entry for ${route.name}.`);
    }

    if (!route.readySelector || !route.landmarkRoot || route.requiredSections.length === 0) {
      throw new Error(`${SPECIMEN_OWNER}: specimen route ${route.name} is blank or missing required selectors.`);
    }

    paths.add(route.path);
    names.add(route.name);
  }

  const expectedMatrix = [
    'light:compact',
    'light:comfortable',
    'light:roomy',
    'dark:compact',
    'dark:comfortable',
    'dark:roomy',
  ];
  const actualMatrix = manifest.themeDensityCombinations.map((x) => `${x.theme}:${x.density}`).sort();
  if (actualMatrix.join('|') !== expectedMatrix.sort().join('|')) {
    throw new Error(`${SPECIMEN_OWNER}: visual matrix must contain exactly Light/Dark x Compact/Comfortable/Roomy.`);
  }
};

export const getSpecimenRoute = (name: SpecimenRoute['name']): SpecimenRoute => {
  const route = specimenManifest.routes.find((candidate) => candidate.name === name);
  if (!route) {
    throw new Error(`${SPECIMEN_OWNER}: missing manifest entry for ${name}.`);
  }

  return route;
};
