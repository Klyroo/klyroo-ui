// A GET form with a field per filter (the filter panel statically rendered) would send every blank field as an empty parameter (pnl.f.customer=&pnl.f.issued=), which fills the
// address with filters that are off. The fields marked data-bx-prune (the ones whose filter has no default rule, where "blank" and "absent" mean the same) are switched off while the
// form is serialized, so the address holds only what was filled in. Without the script the blank parameters are sent and read as "no filter": the result is the same.

/**
 * Switches off the blank marked fields of a form and returns the function that switches them back on, to run once the form data has been taken.
 * @param {{ querySelectorAll(selector: string): Iterable<{ value: string, disabled: boolean }> }} form
 * @returns {() => void}
 */
export function pruneBlank(form) {
  const blanks = [];
  for (const field of form.querySelectorAll('[data-bx-prune]')) {
    if (field.disabled || field.value !== '') continue;
    field.disabled = true;
    blanks.push(field);
  }
  return () => {
    for (const field of blanks) field.disabled = false;
  };
}
