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
