// Semantic Agent Runtime (snapshot, query, act), Unified Observe Stream, and WebMCP integration for Lux.UI.
// Inspired by ui-sdk.dev (@ui-sdk/core/agent and @ui-sdk/core/observe).

const observers = new Set();
const elementRefs = new WeakMap();
const refToElement = new Map();
let refCounter = 0;

/** Normalizes text for accent- and case-insensitive label/query matching. */
function normalizeLabel(text) {
  if (!text) return '';
  return String(text)
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .trim()
    .toLowerCase()
    .replace(/\s+/g, ' ');
}

/** Returns true when an element holds sensitive data (password, OTP/PIN) whose value must be redacted in observe/snapshot. */
function isSensitiveElement(el) {
  if (!el || !(el instanceof Element)) return false;
  if (el.hasAttribute('data-agent-redact')) return true;
  if (el.localName === 'lx-pin-input') return true;
  if (el instanceof HTMLInputElement && el.type === 'password') return true;
  const input = el.querySelector?.('input[type="password"]');
  return Boolean(input);
}

/** Resolves the accessible label of an element. */
export function labelOf(el) {
  if (!el || !(el instanceof Element)) return '';
  const ariaLabel = el.getAttribute('aria-label');
  if (ariaLabel) return ariaLabel.trim();
  const labelledBy = el.getAttribute('aria-labelledby');
  if (labelledBy) {
    const text = labelledBy
      .split(/\s+/)
      .map((id) => document.getElementById(id)?.textContent?.trim())
      .filter(Boolean)
      .join(' ');
    if (text) return text;
  }
  if (el.id) {
    const labelEl = document.querySelector(`label[for="${CSS.escape(el.id)}"]`);
    if (labelEl) return labelEl.textContent.replace(/\*/g, '').trim();
  }
  const wrappingLabel = el.closest('label');
  if (wrappingLabel) {
    const clone = wrappingLabel.cloneNode(true);
    for (const ctrl of clone.querySelectorAll('input, select, textarea, button')) ctrl.remove();
    const t = clone.textContent.replace(/\*/g, '').trim();
    if (t) return t;
  }
  const fieldContainer = el.closest('[data-form-field], [data-slot="form-field"]');
  if (fieldContainer) {
    const lbl = fieldContainer.querySelector('[data-slot="form-field-label"], label');
    if (lbl) return lbl.textContent.replace(/\*/g, '').trim();
  }
  return el.getAttribute('placeholder') || el.getAttribute('title') || el.getAttribute('name') || el.getAttribute('data-form-field') || '';
}

function assignRef(el) {
  let ref = elementRefs.get(el);
  if (!ref) {
    ref = `c${++refCounter}`;
    elementRefs.set(el, ref);
    refToElement.set(ref, el);
  }
  return ref;
}

/** Emits an ObservedChange event to all registered `Lux.observe` subscribers. */
export function emitObservedChange(change) {
  const redacted = change.sensitive
    ? { ...change, value: '[redacted]', previous: '[redacted]' }
    : change;
  for (const entry of observers) {
    if (entry.within && change.element && !entry.within.contains(change.element)) continue;
    try {
      entry.callback(redacted);
    } catch {
      // Subscriber errors never break UI interactions.
    }
  }
}

/**
 * Subscribes to component changes across the page or within a container element.
 * Returns an unsubscribe function.
 */
export function observe(callback, { within = null } = {}) {
  const entry = { callback, within };
  observers.add(entry);
  return () => observers.delete(entry);
}

/** Discovers all agent-addressable components on the page (or within a container). */
function discoverComponents(root = document) {
  if (typeof document === 'undefined') return [];
  const selector = [
    '[data-lx-primitive]',
    'lx-select',
    'lx-select-menu',
    'lx-tabs',
    'lx-modal',
    'lx-collapsible',
    'lx-accordion',
    'lx-pin-input',
    'lx-color-picker',
    'lx-calendar',
    '[data-slot="datalist"]',
    '[data-slot="dashboard-filter"]',
    '[data-slot="form-engine"]',
    '[data-slot="spreadsheet-import"]',
    'input:not([type="hidden"])',
    'select',
    'textarea',
    'button',
  ].join(', ');

  const seen = new Set();
  const list = [];
  for (const el of root.querySelectorAll(selector)) {
    if (seen.has(el)) continue;
    // Skip inner native controls that belong to a higher-level lx-* custom element already in the list
    const ownerLx = el.parentElement?.closest('lx-select, lx-select-menu, lx-pin-input, lx-color-picker');
    if (ownerLx && seen.has(ownerLx)) continue;
    seen.add(el);
    list.push(el);
  }
  return list;
}

function describeElement(el) {
  const ref = assignRef(el);
  const sensitive = isSensitiveElement(el);
  const label = labelOf(el);
  const disabled = el.hasAttribute('disabled') || el.getAttribute('aria-disabled') === 'true';
  const offPage = !el.isConnected || el.closest('[hidden], dialog:not([open])') !== null;

  if (el.__lxPrimitive) {
    const prim = el.__lxPrimitive;
    const st = prim.store.get();
    const actions = prim.agent?.actions ?? Object.keys(prim.api).filter((k) => !['keydown', 'pointerdown', 'wheel'].includes(k));
    return {
      ref,
      component: prim.name,
      label,
      value: sensitive ? '[redacted]' : (st.value ?? st.open ?? st.muted ?? null),
      disabled,
      offPage,
      actions,
    };
  }

  const tag = el.localName;
  if (tag === 'select' || tag === 'lx-select' || tag === 'lx-select-menu') {
    const val = el.value ?? el.getAttribute('value') ?? '';
    return {
      ref,
      component: 'select',
      label,
      value: val,
      disabled,
      offPage,
      actions: ['setValue', 'clear', 'open', 'close'],
    };
  }

  if (tag === 'input' || tag === 'textarea' || tag === 'lx-pin-input') {
    const type = el.getAttribute('type') || 'text';
    if (type === 'checkbox' || type === 'radio') {
      return {
        ref,
        component: type,
        label,
        checked: Boolean(el.checked),
        disabled,
        offPage,
        actions: ['toggle', 'setChecked'],
      };
    }
    return {
      ref,
      component: 'input',
      label,
      value: sensitive ? '[redacted]' : (el.value ?? ''),
      disabled,
      offPage,
      actions: ['setValue', 'clear', 'focus'],
    };
  }

  if (tag === 'button') {
    return {
      ref,
      component: 'button',
      label: label || el.textContent.trim(),
      disabled,
      offPage,
      actions: ['press'],
    };
  }

  return {
    ref,
    component: el.getAttribute('data-slot') || tag,
    label,
    value: el.getAttribute('value') ?? null,
    disabled,
    offPage,
    actions: ['focus'],
  };
}

function extractChoices(el) {
  if (el instanceof HTMLSelectElement) {
    return [...el.options]
      .filter((o) => o.value !== '')
      .map((o) => ({ value: o.value, label: o.text.trim(), disabled: o.disabled, selected: o.selected }));
  }
  const options = el.querySelectorAll('[role="option"], option[value]:not([value=""])');
  return [...options].map((o) => ({
    value: o.getAttribute('data-value') ?? o.getAttribute('value') ?? o.textContent.trim(),
    label: o.textContent.trim(),
    disabled: o.hasAttribute('disabled') || o.getAttribute('aria-disabled') === 'true',
    selected: o.getAttribute('aria-selected') === 'true' || Boolean(o.selected),
  }));
}

/** Creates the Lux Agent runtime (`snapshot`, `query`, `act`, `registerWebMcp`). */
export function createAgent({ allow = null } = {}) {
  function snapshot({ within = null } = {}) {
    const root = within instanceof Element ? within : document;
    const elements = discoverComponents(root);
    const items = elements.slice(0, 100).map(describeElement);
    if (elements.length > 100) {
      items.push({ more: elements.length - 100 });
    }
    return items;
  }

  async function query({ within = null, text = '', kind = '' } = {}) {
    const elements = discoverComponents(document);
    const normWithin = normalizeLabel(within);
    const normText = normalizeLabel(text);
    const normKind = normalizeLabel(kind);

    if (normWithin) {
      const target = elements.find((el) => {
        const ref = elementRefs.get(el);
        return ref === within || normalizeLabel(labelOf(el)) === normWithin;
      });
      if (!target) {
        return { error: `No component found matching "${within}".` };
      }
      const allChoices = extractChoices(target);
      const filtered = normText
        ? allChoices.filter((c) => normalizeLabel(c.label).includes(normText) || normalizeLabel(c.value).includes(normText))
        : allChoices;
      return [
        {
          ...describeElement(target),
          items: filtered.slice(0, 50),
          more: Math.max(0, filtered.length - 50),
        },
      ];
    }

    const matches = [];
    for (const el of elements) {
      const desc = describeElement(el);
      if (normKind && normalizeLabel(desc.component) !== normKind) continue;
      const choices = extractChoices(el);
      const matchingChoices = normText
        ? choices.filter((c) => normalizeLabel(c.label).includes(normText) || normalizeLabel(c.value).includes(normText))
        : [];
      const labelMatches = !normText || normalizeLabel(desc.label).includes(normText);
      if (labelMatches || matchingChoices.length > 0) {
        matches.push({
          ...desc,
          ...(matchingChoices.length > 0 ? { items: matchingChoices.slice(0, 50), more: Math.max(0, matchingChoices.length - 50) } : {}),
        });
      }
    }
    return matches.slice(0, 50);
  }

  async function act({ steps = [], allow: callAllow = null } = {}) {
    const appliedSteps = [];
    const values = {};

    for (let i = 0; i < steps.length; i++) {
      const step = steps[i];
      const elements = discoverComponents(document);
      const normRef = normalizeLabel(step.ref);
      const matches = elements.filter((el) => {
        const r = assignRef(el);
        if (r === step.ref) return true;
        return normalizeLabel(labelOf(el) || el.textContent) === normRef;
      });

      if (matches.length === 0) {
        return { ok: false, steps: appliedSteps, values, error: `Step ${i + 1}: component "${step.ref}" not found.` };
      }
      if (matches.length > 1 && !refToElement.has(step.ref)) {
        return {
          ok: false,
          steps: appliedSteps,
          values,
          error: `Step ${i + 1}: ambiguous label "${step.ref}" matched ${matches.length} components (${matches.map(assignRef).join(', ')}). Use a specific ref.`,
        };
      }

      const el = matches[0];
      const ref = assignRef(el);
      const needsConfirm = el.hasAttribute('data-agent-confirm') || el.closest('[data-agent-confirm]') !== null;
      const stepWithMeta = { ...step, ref, confirm: needsConfirm };

      if (allow && (await allow(stepWithMeta)) === false) {
        return { ok: false, steps: appliedSteps, values, error: `Step ${i + 1}: action "${step.action}" refused by agent policy.` };
      }
      if (callAllow && (await callAllow(stepWithMeta)) === false) {
        return { ok: false, steps: appliedSteps, values, error: `Step ${i + 1}: action "${step.action}" refused by caller policy.` };
      }

      const prevValue = el.value ?? el.getAttribute('value') ?? null;
      const args = step.args ?? [];

      if (el.__lxPrimitive && typeof el.__lxPrimitive.api[step.action] === 'function') {
        el.__lxPrimitive.api[step.action](...args);
      } else if (step.action === 'setValue') {
        const nextVal = args[0] ?? '';
        if ('value' in el) el.value = nextVal;
        el.setAttribute('value', String(nextVal));
        el.dispatchEvent(new Event('input', { bubbles: true }));
        el.dispatchEvent(new Event('change', { bubbles: true }));
        el.dispatchEvent(new CustomEvent('lx-change', { bubbles: true, detail: { value: nextVal, source: 'agent' } }));
      } else if (step.action === 'clear') {
        if ('value' in el) el.value = '';
        el.dispatchEvent(new Event('input', { bubbles: true }));
        el.dispatchEvent(new Event('change', { bubbles: true }));
      } else if (step.action === 'toggle' || step.action === 'setChecked') {
        if ('checked' in el) {
          el.checked = step.action === 'setChecked' ? Boolean(args[0]) : !el.checked;
          el.dispatchEvent(new Event('change', { bubbles: true }));
        } else {
          el.click();
        }
      } else if (step.action === 'press') {
        el.click();
      } else if (step.action === 'focus') {
        el.focus();
      } else {
        return { ok: false, steps: appliedSteps, values, error: `Step ${i + 1}: unsupported action "${step.action}" on ${ref}.` };
      }

      const newValue = el.value ?? el.getAttribute('value') ?? null;
      const sensitive = isSensitiveElement(el);
      emitObservedChange({
        component: describeElement(el).component,
        ref,
        label: labelOf(el),
        key: 'value',
        previous: prevValue,
        value: newValue,
        reason: step.action,
        source: 'agent',
        sensitive,
        element: el,
      });

      values[ref] = sensitive ? '[redacted]' : newValue;
      appliedSteps.push({ ref, action: step.action, ok: true });
    }

    return { ok: true, steps: appliedSteps, values };
  }

  return {
    snapshot,
    query,
    act,
    observe,
    agentTools,
    registerWebMcp: (options) => registerWebMcp({ snapshot, query, act }, options),
  };
}

/** Standard tool definitions for LLM / MCP / WebMCP integration. */
export const agentTools = [
  {
    name: 'snapshot',
    description: 'Lists the Lux.UI components on the page, their current state, and the actions they support.',
    annotations: { readOnlyHint: true, untrustedContentHint: true },
    inputSchema: {
      type: 'object',
      properties: {},
    },
  },
  {
    name: 'query',
    description: 'Finds components by label or kind, or lists available options inside a select, filter, or data list without opening popups.',
    annotations: { readOnlyHint: true, untrustedContentHint: true },
    inputSchema: {
      type: 'object',
      properties: {
        within: { type: 'string', description: 'Optional component ref or label to inspect choices within.' },
        text: { type: 'string', description: 'Optional search text to filter components or choices.' },
        kind: { type: 'string', description: 'Optional component kind filter (e.g. select, input, button).' },
      },
    },
  },
  {
    name: 'act',
    description: 'Calls one or more semantic actions on components by ref or label (e.g. setValue, clear, toggle, press).',
    annotations: { consequentialHint: true, untrustedContentHint: true },
    inputSchema: {
      type: 'object',
      required: ['steps'],
      properties: {
        steps: {
          type: 'array',
          items: {
            type: 'object',
            required: ['ref', 'action'],
            properties: {
              ref: { type: 'string', description: 'Component ref (e.g. c1) or accessible label.' },
              action: { type: 'string', description: 'Action name from snapshot (e.g. setValue, clear, toggle, press).' },
              args: { type: 'array', items: {}, description: 'Arguments for the action.' },
            },
          },
        },
      },
    },
  },
];

/** Registers `snapshot`, `query`, and `act` with the browser's WebMCP (`document.modelContext.registerTool`) API if available. */
export function registerWebMcp(agent, { allow = null } = {}) {
  if (typeof document === 'undefined') return () => {};
  const mc = document.modelContext ?? globalThis.navigator?.modelContext;
  if (!mc || typeof mc.registerTool !== 'function') return () => {};

  const cleanups = [];
  for (const tool of agentTools) {
    const unregister = mc.registerTool({
      ...tool,
      execute: async (args) => {
        if (tool.name === 'snapshot') return agent.snapshot(args);
        if (tool.name === 'query') return agent.query(args);
        if (tool.name === 'act') return agent.act({ ...args, allow });
        return null;
      },
    });
    if (typeof unregister === 'function') cleanups.push(unregister);
  }
  return () => {
    for (const fn of cleanups) fn();
  };
}
