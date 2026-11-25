namespace Klyroo.Templates.Content;

/// <summary>
/// The script behind the "Copy" button of a code block. It is a few lines of plain JavaScript that listen for clicks on the whole page, so it works
/// on pages that are only HTML (no circuit, no WebAssembly) and on content that arrives later. Put it once in the document, for example
/// <c>&lt;script&gt;@((MarkupString)CodeCopy.Script)&lt;/script&gt;</c>; a site with a Content-Security-Policy gives it a nonce or serves it as a file instead.
/// </summary>
public static class CodeCopy
{
    public const string Script = """
        (() => {
          const timers = new WeakMap();
          const fallback = (text) => {
            const area = document.createElement("textarea");
            area.value = text;
            area.setAttribute("readonly", "");
            area.style.position = "fixed";
            area.style.opacity = "0";
            document.body.appendChild(area);
            area.select();
            const ok = document.execCommand("copy");
            area.remove();
            if (!ok) throw new Error("copy failed");
          };
          // The clipboard API needs a secure context and, in some browsers, a permission; the old way works on a click everywhere.
          const copy = async (text) => {
            try {
              await navigator.clipboard.writeText(text);
            } catch {
              fallback(text);
            }
          };
          document.addEventListener("click", async (event) => {
            const button = event.target instanceof Element ? event.target.closest("[data-code-copy]") : null;
            if (!button) return;
            const block = button.closest(".code-block");
            const code = block && block.querySelector("pre code");
            const status = block && block.querySelector(".code-copy-status");
            if (!code) return;
            let label = "Copied";
            try {
              await copy((code.textContent || "").replace(/\n$/, ""));
            } catch {
              label = "Copy failed";
            }
            button.focus();
            button.textContent = label;
            if (status) status.textContent = label;
            clearTimeout(timers.get(button));
            timers.set(button, setTimeout(() => {
              button.textContent = "Copy";
              if (status) status.textContent = "";
            }, 2000));
          });
        })();
        """;
}
