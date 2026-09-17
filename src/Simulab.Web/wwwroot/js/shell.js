// App shell (F-2). Plain script: works before the Blazor circuit starts.
window.simulabShell = {
  // Preference cookies are written from the browser so switching needs no page reload.
  setPreference: function (name, value, maxAgeSeconds) {
    document.cookie = encodeURIComponent(name) + "=" + encodeURIComponent(value)
      + "; max-age=" + maxAgeSeconds + "; path=/; samesite=lax" + (location.protocol === "https:" ? "; secure" : "");
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
