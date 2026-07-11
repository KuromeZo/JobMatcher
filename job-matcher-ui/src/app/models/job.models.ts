export interface RequiredSkill {
  name: string;
  level: number;
}

export interface EmploymentType {
  from: number | null;
  to: number | null;
  currency: string;
  type: string;
  unit: string;
}

export interface JobOffer {
  guid: string;
  slug: string;
  title: string;
  companyName: string;
  city: string;
  workplaceType: string;
  experienceLevel: string;
  category: string;
  requiredSkills: RequiredSkill[];
  niceToHaveSkills: RequiredSkill[];
  employmentTypes: EmploymentType[];
  publishedAt: string;
}

export interface ScoredJob {
  offer: JobOffer;
  score: number;
  matches: string[];
  toLearn: string[];
  verdict: string;
}

export interface ScoreResponse {
  total: number;
  jobs: ScoredJob[];
}
