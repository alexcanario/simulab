// Shell preferences (F-2): written from the browser so switching needs no page reload.
window.simulabPreferences = {
  set: function (name, value, maxAgeSeconds) {
    document.cookie = encodeURIComponent(name) + "=" + encodeURIComponent(value)
      + "; max-age=" + maxAgeSeconds + "; path=/; samesite=lax" + (location.protocol === "https:" ? "; secure" : "");
  }
};
