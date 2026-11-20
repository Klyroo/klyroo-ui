/**
 * Spatial navigation geometry, free of the DOM so Node can test it. A rectangle is { left, top, right, bottom }.
 */

/** The four arrow keys and the direction each one moves. */
export const DIRECTIONS = {
  ArrowLeft: 'left',
  ArrowRight: 'right',
  ArrowUp: 'up',
  ArrowDown: 'down',
};

function overlap(aStart, aEnd, bStart, bEnd) {
  return Math.max(0, Math.min(aEnd, bEnd) - Math.max(aStart, bStart));
}

/**
 * How far a candidate is from the origin in a direction, or null when it does not lie that way. The score favours the
 * nearest element in the direction of travel and punishes sideways drift; an element that shares no row (or column)
 * with the origin pays a penalty, so a neighbour in line wins over a closer one that is far off to the side.
 */
export function score(origin, candidate, direction) {
  const horizontal = direction === 'left' || direction === 'right';
  // Distance along the axis of travel, between the facing edges. A small overlap tolerance (1px) keeps adjacent boxes.
  let along;
  switch (direction) {
    case 'left':
      along = origin.left - candidate.right;
      break;
    case 'right':
      along = candidate.left - origin.right;
      break;
    case 'up':
      along = origin.top - candidate.bottom;
      break;
    default:
      along = candidate.top - origin.bottom;
      break;
  }
  if (along < -1) {
    // Overlapping along the axis: still a valid target when its center lies beyond the origin's center in that direction.
    const oc = center(origin);
    const cc = center(candidate);
    const beyond = { left: cc.x < oc.x, right: cc.x > oc.x, up: cc.y < oc.y, down: cc.y > oc.y }[direction];
    if (!beyond) return null;
    along = 0;
  }
  along = Math.max(0, along);

  const shared = horizontal
    ? overlap(origin.top, origin.bottom, candidate.top, candidate.bottom)
    : overlap(origin.left, origin.right, candidate.left, candidate.right);
  // A candidate that shares a row (or column) with the origin is in line: no sideways cost, whatever their sizes, so a
  // small control aligned under a wide field is reached before a farther one. Otherwise the cost is the gap between
  // the edges across the axis, plus a penalty so that any in-line candidate wins.
  if (shared > 0) return along;
  const across = horizontal
    ? Math.max(candidate.top - origin.bottom, origin.top - candidate.bottom)
    : Math.max(candidate.left - origin.right, origin.left - candidate.right);
  return along + 2 * Math.max(0, across) + 1000;
}

function center(rect) {
  return { x: (rect.left + rect.right) / 2, y: (rect.top + rect.bottom) / 2 };
}

/**
 * The index of the candidate to move to, or -1. Candidates are rectangles; the origin is excluded by the caller.
 */
export function pickNext(origin, candidates, direction) {
  let best = -1;
  let bestScore = Infinity;
  for (let i = 0; i < candidates.length; i++) {
    const value = score(origin, candidates[i], direction);
    if (value !== null && value < bestScore) {
      best = i;
      bestScore = value;
    }
  }
  return best;
}
