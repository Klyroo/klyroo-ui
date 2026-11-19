// The TipTap side of <bx-editor>: bundled apart (blux-editor.js) and loaded with a dynamic import by js/editor.js,
// only when an editor element connects. Everything that needs ProseMirror lives here; the elements in editor.js
// only deal with the DOM that Blazor renders.
import { Editor, Extension } from '@tiptap/core';
import StarterKit from '@tiptap/starter-kit';
import { Placeholder } from '@tiptap/extension-placeholder';
import Mention from '@tiptap/extension-mention';
import Suggestion from '@tiptap/suggestion';
import { NodeSelection, PluginKey, TextSelection } from '@tiptap/pm/state';

// The mention node without TipTap's own suggestion plugin: menus are registered from the DOM (registerSuggestion).
const MentionNode = Mention.extend({
  addProseMirrorPlugins() {
    return [];
  },
});

const TOGGLES = new Set(['bold', 'italic', 'strike', 'underline', 'code', 'heading', 'bulletList', 'orderedList', 'blockquote', 'codeBlock', 'link']);

/** Whether an action is a toggle, so its button shows aria-pressed. */
export const isToggle = (action) => TOGGLES.has(action);

/**
 * @param {object} options
 * @param {HTMLElement} options.element Empty element the editable surface is appended to.
 * @param {string} options.html Initial content.
 * @param {boolean} options.editable
 * @param {() => string} options.placeholder
 * @param {Record<string, string>} options.attributes Attributes of the editable element (role, aria-*).
 * @param {() => void} options.onUpdate Content changed.
 * @param {() => void} options.onState Selection, focus or content changed: toolbars repaint.
 * @param {() => void} options.onBlur
 */
export function createEngine({ element, html, editable, placeholder, attributes, onUpdate, onState, onBlur }) {
  let bump = 0;
  const notify = () => {
    cancelAnimationFrame(bump);
    bump = requestAnimationFrame(onState);
  };

  const editor = new Editor({
    element,
    content: html,
    editable,
    extensions: [
      StarterKit.configure({
        link: { openOnClick: false, autolink: true, defaultProtocol: 'https', HTMLAttributes: { rel: 'noopener noreferrer nofollow', target: null } },
      }),
      Placeholder.configure({ placeholder: () => placeholder(), showOnlyCurrent: true }),
      MentionNode.configure({ HTMLAttributes: { class: 'bx-mention' } }),
      Extension.create({
        name: 'bxMoveBlock',
        addKeyboardShortcuts: () => ({
          'Alt-ArrowUp': () => moveSelection(-1),
          'Alt-ArrowDown': () => moveSelection(1),
        }),
      }),
    ],
    editorProps: { attributes },
    onUpdate: () => {
      onUpdate();
      notify();
    },
    onSelectionUpdate: notify,
    onTransaction: notify,
    onFocus: notify,
    onBlur: () => {
      onBlur();
      notify();
    },
  });

  const view = () => editor.view;

  // ---- Blocks

  /** Start position of the top-level block holding the selection, or null. */
  function selectionBlock() {
    const { selection } = editor.state;
    if (selection instanceof NodeSelection && selection.$from.depth === 0) return selection.from;
    return selection.$from.depth >= 1 ? selection.$from.before(1) : null;
  }

  /** Moves the top-level block starting at pos one place up (-1) or down (1). Returns its new start, or -1. */
  function moveBlock(pos, direction, keepSelection) {
    const { state } = editor;
    const { doc } = state;
    if (pos == null || pos < 0 || pos > doc.content.size) return -1;
    const index = doc.resolve(pos).index(0);
    const target = index + direction;
    if (target < 0 || target >= doc.childCount) return -1;
    const node = doc.child(index);
    const other = doc.child(target);
    const end = pos + node.nodeSize;
    const { selection } = state;
    const tr = state.tr;
    let start;
    if (direction < 0) {
      start = pos - other.nodeSize;
      tr.delete(pos, end).insert(start, node);
    } else {
      start = pos + other.nodeSize;
      tr.insert(end + other.nodeSize, node).delete(pos, end);
    }
    try {
      if (!keepSelection || selection instanceof NodeSelection) tr.setSelection(NodeSelection.create(tr.doc, start));
      else tr.setSelection(TextSelection.create(tr.doc, start + (selection.anchor - pos), start + (selection.head - pos)));
    } catch {
      // The selection reaches outside the block: the content moved, the selection stays where the transaction put it.
    }
    editor.view.dispatch(tr.scrollIntoView());
    return start;
  }

  function moveSelection(direction) {
    const pos = selectionBlock();
    if (pos == null) return false;
    const { selection } = editor.state;
    const within = selection.from >= pos && selection.to <= pos + editor.state.doc.nodeAt(pos).nodeSize;
    return moveBlock(pos, direction, within) >= 0;
  }

  // ---- Actions

  const chain = () => editor.chain().focus();

  function run(action, arg) {
    switch (action) {
      case 'bold': return chain().toggleBold().run();
      case 'italic': return chain().toggleItalic().run();
      case 'strike': return chain().toggleStrike().run();
      case 'underline': return chain().toggleUnderline().run();
      case 'code': return chain().toggleCode().run();
      case 'paragraph': return chain().setParagraph().run();
      case 'heading': return chain().toggleHeading({ level: Number(arg) || 1 }).run();
      case 'bulletList': return chain().toggleBulletList().run();
      case 'orderedList': return chain().toggleOrderedList().run();
      case 'blockquote': return chain().toggleBlockquote().run();
      case 'codeBlock': return chain().toggleCodeBlock().run();
      case 'horizontalRule': return chain().setHorizontalRule().run();
      case 'clearFormatting': return chain().unsetAllMarks().clearNodes().run();
      case 'undo': return chain().undo().run();
      case 'redo': return chain().redo().run();
      case 'moveUp': return moveSelection(-1);
      case 'moveDown': return moveSelection(1);
      case 'link':
        return arg
          ? chain().extendMarkRange('link').setLink({ href: arg }).run()
          : chain().extendMarkRange('link').unsetLink().run();
      default: return false;
    }
  }

  function isActive(action, arg) {
    switch (action) {
      case 'heading': return editor.isActive('heading', { level: Number(arg) || 1 });
      case 'paragraph': return editor.isActive('paragraph');
      default: return isToggle(action) ? editor.isActive(action) : false;
    }
  }

  function can(action) {
    if (!editor.isEditable) return false;
    switch (action) {
      case 'undo': return editor.can().undo();
      case 'redo': return editor.can().redo();
      case 'moveUp':
      case 'moveDown': {
        const pos = selectionBlock();
        if (pos == null) return false;
        const index = editor.state.doc.resolve(pos).index(0);
        return action === 'moveUp' ? index > 0 : index < editor.state.doc.childCount - 1;
      }
      default: return true;
    }
  }

  // ---- Suggestions

  /** Applies what the user chose in a suggestion menu in place of the typed trigger and query. */
  function apply(range, choice) {
    if (choice.type === 'mention') {
      editor.chain().focus().insertContentAt(range, [{ type: 'mention', attrs: { id: choice.id, label: choice.label } }, { type: 'text', text: ' ' }]).run();
    } else if (choice.type === 'text') {
      editor.chain().focus().insertContentAt(range, choice.text).run();
    } else {
      editor.chain().focus().deleteRange(range).run();
      run(choice.action, choice.level);
    }
  }

  /**
   * Registers a suggestion menu: spec has id, char, startOfLine, onStart/onUpdate({ query, rect, select }), onExit(),
   * onKeyDown(event). Returns the function that removes it.
   */
  function registerSuggestion(spec) {
    const key = new PluginKey(`bx-${spec.id}`);
    const context = (props) => ({ query: props.query, rect: () => props.clientRect?.() ?? null, select: (choice) => props.command(choice) });
    const plugin = Suggestion({
      editor,
      pluginKey: key,
      char: spec.char,
      startOfLine: spec.startOfLine ?? false,
      items: () => [],
      command: ({ range, props }) => apply(range, props),
      render: () => ({
        onStart: (props) => spec.onStart(context(props)),
        onUpdate: (props) => spec.onUpdate(context(props)),
        onExit: () => spec.onExit(),
        onKeyDown: ({ event }) => spec.onKeyDown(event),
      }),
    });
    // First in line: Enter and Tab belong to the menu while it is open, before the editor's own key bindings.
    editor.registerPlugin(plugin, (added, plugins) => [added, ...plugins]);
    return () => {
      if (!editor.isDestroyed) editor.unregisterPlugin(key);
    };
  }

  // ---- Drag handle

  /**
   * Shows `container` beside the block under the pointer (or holding the selection) and lets `button` drag it.
   * Positions are relative to `host`. Moving a block with the keyboard is moveUp/moveDown, Alt+Arrow in the text, or
   * the arrow keys on the focused handle.
   */
  function attachDragHandle(host, container, button) {
    let current = -1;

    const blockAt = (clientX, clientY) => {
      const box = view().dom.getBoundingClientRect();
      const hit = view().posAtCoords({ left: Math.min(Math.max(clientX, box.left + 1), box.right - 1), top: clientY });
      if (!hit) return -1;
      const spot = hit.inside >= 0 ? hit.inside : hit.pos;
      const $spot = editor.state.doc.resolve(Math.min(spot, editor.state.doc.content.size));
      return $spot.depth >= 1 ? $spot.before(1) : hit.inside >= 0 ? hit.inside : -1;
    };

    const place = (pos) => {
      const dom = pos >= 0 ? view().nodeDOM(pos) : null;
      if (!(dom instanceof HTMLElement)) {
        current = -1;
        container.hidden = true;
        return;
      }
      current = pos;
      const base = host.getBoundingClientRect();
      const block = dom.getBoundingClientRect();
      const surface = view().dom.getBoundingClientRect();
      container.hidden = false;
      const first = Number.parseFloat(getComputedStyle(dom).lineHeight) || 24;
      container.style.top = `${block.top - base.top + Math.max(0, (first - container.offsetHeight) / 2)}px`;
      container.style.left = `${surface.left - base.left - container.offsetWidth - 4}px`;
    };

    const restore = () => {
      const focused = editor.isFocused || container.contains(document.activeElement);
      place(focused ? (selectionBlock() ?? -1) : -1);
    };

    const onMove = (event) => {
      if (!editor.isEditable) return;
      const pos = blockAt(event.clientX, event.clientY);
      if (pos >= 0) place(pos);
    };

    // Selects the block without taking focus from the handle: the arrow keys on it move the block.
    const selectBlock = () => {
      if (current < 0) return;
      view().dispatch(editor.state.tr.setSelection(NodeSelection.create(editor.state.doc, current)));
      button.focus();
    };

    const onKeyDown = (event) => {
      if (current < 0 || !editor.isEditable) return;
      if (event.key === 'ArrowUp' || event.key === 'ArrowDown') {
        event.preventDefault();
        const moved = moveBlock(current, event.key === 'ArrowUp' ? -1 : 1, false);
        if (moved >= 0) {
          place(moved);
          button.focus();
        }
      } else if (event.key === 'Enter' || event.key === ' ') {
        event.preventDefault();
        selectBlock();
      }
    };

    const onDragStart = (event) => {
      if (current < 0 || !editor.isEditable) {
        event.preventDefault();
        return;
      }
      const selection = NodeSelection.create(editor.state.doc, current);
      view().dispatch(editor.state.tr.setSelection(selection));
      view().dragging = { slice: selection.content(), move: true };
      const dom = view().nodeDOM(current);
      event.dataTransfer.effectAllowed = 'copyMove';
      event.dataTransfer.setData('text/plain', dom?.textContent ?? '');
      if (dom instanceof HTMLElement) event.dataTransfer.setDragImage(dom, 0, 0);
    };

    const onDragEnd = () => {
      view().dragging = null;
      restore();
    };

    const stopMove = (event) => event.stopPropagation();
    const onBlur = () => setTimeout(restore, 0);

    const editorDom = view().dom;
    editorDom.addEventListener('mousemove', onMove);
    host.addEventListener('mouseleave', restore);
    container.addEventListener('mousemove', stopMove);
    button.addEventListener('click', selectBlock);
    button.addEventListener('keydown', onKeyDown);
    button.addEventListener('dragstart', onDragStart);
    button.addEventListener('dragend', onDragEnd);
    button.draggable = true;
    editor.on('selectionUpdate', restore);
    editor.on('focus', restore);
    editor.on('blur', onBlur);

    // The handle can be removed and added again (Blazor moves and replaces elements): every listener goes, or they stack.
    return () => {
      editorDom.removeEventListener('mousemove', onMove);
      host.removeEventListener('mouseleave', restore);
      container.removeEventListener('mousemove', stopMove);
      button.removeEventListener('click', selectBlock);
      button.removeEventListener('keydown', onKeyDown);
      button.removeEventListener('dragstart', onDragStart);
      button.removeEventListener('dragend', onDragEnd);
      editor.off('selectionUpdate', restore);
      editor.off('focus', restore);
      editor.off('blur', onBlur);
      container.hidden = true;
    };
  }

  // ---- Selection

  /** Rectangle of the selection on screen (for a bubble toolbar). */
  function selectionRect() {
    const { from, to } = editor.state.selection;
    const start = view().coordsAtPos(from);
    const end = view().coordsAtPos(to);
    const left = Math.min(start.left, end.left);
    const top = Math.min(start.top, end.top);
    return new DOMRect(left, top, Math.max(end.right, start.right) - left, Math.max(end.bottom, start.bottom) - top);
  }

  return {
    dom: editor.view.dom,
    getHTML: () => (editor.isEmpty ? '' : editor.getHTML()),
    setHTML: (value) => editor.commands.setContent(value, { emitUpdate: false }),
    setEditable: (value) => editor.setEditable(value, false),
    refresh: () => view().dispatch(editor.state.tr),
    focus: () => editor.commands.focus(),
    get focused() {
      return editor.isFocused;
    },
    get editable() {
      return editor.isEditable;
    },
    get hasSelection() {
      return !editor.state.selection.empty;
    },
    linkHref: () => editor.getAttributes('link').href ?? '',
    selectionRect,
    run,
    isActive,
    can,
    registerSuggestion,
    attachDragHandle,
    destroy: () => editor.destroy(),
  };
}
