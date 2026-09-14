export const TOUR_STEPS = [
  { id: 'year', textKey: 'tour.step1' },
  { id: 'map', textKey: 'tour.step2' },
  { id: 'evidence', textKey: 'tour.step3' },
];

export function nextTourStep(current) {
  if (current === null || current === undefined) return 0;
  if (current >= TOUR_STEPS.length - 1) return null;
  return current + 1;
}
