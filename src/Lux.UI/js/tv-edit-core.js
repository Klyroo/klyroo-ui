/**
 * Editing mode of TV navigation, without the DOM so it can be tested. On a remote control the arrows must move focus
 * between fields, so a field only takes them after Select/Enter put it in editing mode.
 */

const TEXT_TYPES = new Set(['text', 'search', 'email', 'url', 'tel', 'password', '']);

/**
 * What kind of editing a field has: "text" (a soft keyboard types into it), "step" (number and range: the arrows change the
 * value), "select" (a native select: the arrows pick an option) or null (not a field that needs a mode: buttons,
 * checkboxes, radios, dates).
 */
export function editKind({ tag, type = '', editable = false }) {
  if (tag === 'textarea' || editable) return 'text';
  if (tag === 'select') return 'select';
  if (tag !== 'input') return null;
  if (TEXT_TYPES.has(type)) return 'text';
  if (type === 'number' || type === 'range') return 'step';
  return null;
}

/** Whether the soft keyboard is wanted for this field. A range or a select has nothing to type. */
export function wantsKeyboard(kind, type = '') {
  return kind === 'text' || (kind === 'step' && type === 'number');
}

/** Whether Enter ends editing (a single line) or is typed (a textarea, an editable region). */
export function enterLeaves({ tag, editable = false }) {
  return tag !== 'textarea' && !editable;
}

/** +1 or -1 for the arrows that raise or lower a value, 0 for the others. */
export function stepOf(key) {
  if (key === 'ArrowUp' || key === 'ArrowRight') return 1;
  if (key === 'ArrowDown' || key === 'ArrowLeft') return -1;
  return 0;
}

/**
 * Where the caret goes for an arrow key, because a forwarded key has no default action. Up and Down keep the column
 * (lines are the ones separated by line breaks); with `multiline` false they go to the start or the end of the text.
 */
export function caretAfter(value, caret, key, multiline) {
  switch (key) {
    case 'ArrowLeft': return Math.max(0, caret - 1);
    case 'ArrowRight': return Math.min(value.length, caret + 1);
    case 'ArrowUp':
    case 'ArrowDown': {
      if (!multiline) return key === 'ArrowUp' ? 0 : value.length;
      const lineStart = value.lastIndexOf('\n', caret - 1) + 1;
      const column = caret - lineStart;
      if (key === 'ArrowUp') {
        if (lineStart === 0) return 0;
        const previousStart = value.lastIndexOf('\n', lineStart - 2) + 1;
        return Math.min(previousStart + column, lineStart - 1);
      }
      const lineEnd = value.indexOf('\n', caret);
      if (lineEnd === -1) return value.length;
      const nextEnd = value.indexOf('\n', lineEnd + 1);
      return Math.min(lineEnd + 1 + column, nextEnd === -1 ? value.length : nextEnd);
    }
    default: return caret;
  }
}
