// App shell (F-2). Plain script: works before the Blazor circuit starts.
window.simulabShell = {
  // Preference cookies are written from the browser so switching needs no page reload.
  setPreference: function (name, value, maxAgeSeconds) {
    document.cookie = encodeURIComponent(name) + "=" + encodeURIComponent(value)
      + "; max-age=" + maxAgeSeconds + "; path=/; samesite=lax" + (location.protocol === "https:" ? "; secure" : "");
  },
  // F-11: the recovery codes' Copy action.
  copyText: function (text) {
    return navigator.clipboard.writeText(text);
  },
  // F-14: the browser's IANA time zone, so times are shown where the user is.
  timeZone: function () {
    return Intl.DateTimeFormat().resolvedOptions().timeZone;
  },
  // F-43: the error summary's links. With <base href="/">, following "#exam-name" would leave the page
  // (rule: ui-project), so the jump is done here, the way the skip link below does it.
  focusElement: function (id) {
    var element = document.getElementById(id);
    if (!element) {
      return;
    }
    // A radio group is the field's id but is not focusable itself: move to the card that holds the tab stop.
    var target = element.matches("input, select, textarea, button, a[href], [tabindex]")
      ? element
      : element.querySelector("[tabindex='0'], input, select, textarea, button") || element;
    target.focus();
    target.scrollIntoView({ block: "center" });
  },
  // F-75: moves the focus to one option of a multi-pick list (or back to its input) without jumping the page; the
  // list scrolls just enough to show it.
  focusOption: function (id) {
    var element = document.getElementById(id);
    if (!element) {
      return;
    }
    element.focus({ preventScroll: true });
    element.scrollIntoView({ block: "nearest" });
  },
  // F-75: closes an open multi-pick list when a pointer or the focus lands outside it. The handler removes itself after
  // the first outside event; one left over from a list closed another way only closes an already closed list.
  closeOnOutside: function (rootId, reference) {
    var handler = function (event) {
      var root = document.getElementById(rootId);
      if (root && event.target instanceof Node && root.contains(event.target)) {
        return;
      }
      document.removeEventListener("pointerdown", handler, true);
      document.removeEventListener("focusin", handler, true);
      reference.invokeMethodAsync("CloseFromOutside").catch(function () { });
    };
    document.addEventListener("pointerdown", handler, true);
    document.addEventListener("focusin", handler, true);
  },
  // F-16: saves a file the circuit streamed (DotNetStreamReference); nothing is stored on the server.
  downloadFile: async function (fileName, contentType, streamReference) {
    var buffer = await streamReference.arrayBuffer();
    var url = URL.createObjectURL(new Blob([buffer], { type: contentType }));
    var link = document.createElement("a");
    link.href = url;
    link.download = fileName;
    document.body.appendChild(link);
    link.click();
    link.remove();
    URL.revokeObjectURL(url);
  }
};

// F-75: the multi-pick field's keys. What the browser does with a key (the arrows scroll, Space scrolls a page, Home and End
// move a caret) is decided before the server answers, so it cannot wait for the Blazor handler.
document.addEventListener("keydown", function (event) {
  var root = event.target instanceof Element ? event.target.closest("[data-app-multipick]") : null;
  if (!root || event.ctrlKey || event.altKey || event.metaKey) {
    return;
  }
  var key = event.key;
  if (event.target.getAttribute("role") === "option") {
    if (key === "ArrowDown" || key === "ArrowUp" || key === "Home" || key === "End" || key === " " || key === "Spacebar" || key === "Enter") {
      event.preventDefault();
    } else if (key.length === 1) {
      // Typing in the list goes back to the input: the focus moves now, so this very key is typed there.
      var input = root.querySelector("[role='combobox']");
      if (input && !input.disabled) {
        input.focus();
      }
    }
    return;
  }
  if (event.target.getAttribute("role") === "combobox" && (key === "ArrowDown" || key === "ArrowUp")) {
    event.preventDefault();
  }
}, true);

// Skip link: move focus to the main content. With <base href="/">, following "#main-content" would leave the page.
document.addEventListener("click", function (event) {
  var link = event.target instanceof Element ? event.target.closest(".app-skip-link") : null;
  var main = document.getElementById("main-content");
  if (!link || !main) {
    return;
  }
  event.preventDefault();
  main.focus();
  main.scrollIntoView();
}, true); // capture: runs before Blazor intercepts the link
