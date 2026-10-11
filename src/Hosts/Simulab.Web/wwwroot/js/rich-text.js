// Rich text field (F-95). Plain script, loaded after quill.js and the Blazored.TextEditor adapter.
//
// Blazored.TextEditor creates the Quill instance inside the element the kit's AppRichTextField hosts it in. This
// file is the kit's side of the editor: it finds that instance and gives it the accessible name, the keyboard and
// the formats the item asks for. Nothing else in the app talks to Quill. The Api sanitizes whatever the editor
// holds; this script is comfort and accessibility, never the safety net.
window.simulabRichText = (function () {
  "use strict";

  // The formats the kit offers; they match the allow-list of the Api's sanitizer (F-95 BR8).
  var allowedFormats = ["bold", "italic", "underline", "strike", "script", "list", "blockquote", "link"];
  var pauseMilliseconds = 300;
  var readyAttempts = 100;
  var readyIntervalMilliseconds = 50;

  var editors = {};

  function setBoolean(element, name, on) {
    if (on) {
      element.setAttribute(name, "true");
    } else {
      element.removeAttribute(name);
    }
  }

  function find(host) {
    if (!host || typeof Quill === "undefined") {
      return null;
    }
    var root = host.querySelector(".ql-editor");
    if (!root || !root.parentElement) {
      return null;
    }
    var quill = Quill.find(root.parentElement);
    if (!quill) {
      return null;
    }
    return { quill: quill, root: root, toolbar: host.querySelector("[role='toolbar']") };
  }

  function applyAttributes(entry) {
    var options = entry.options;
    var root = entry.root;
    root.id = options.id;
    root.setAttribute("role", "textbox");
    root.setAttribute("aria-multiline", "true");
    root.setAttribute("aria-labelledby", options.labelId);
    root.setAttribute("aria-describedby", options.descriptionId);
    setBoolean(root, "aria-required", options.required);
    setBoolean(root, "aria-invalid", options.invalid);
    setBoolean(root, "aria-disabled", options.disabled);
    if (options.lang) {
      root.setAttribute("lang", options.lang);
    } else {
      root.removeAttribute("lang");
    }
    if (options.placeholder) {
      root.setAttribute("data-placeholder", options.placeholder);
    }
    entry.quill.enable(!options.disabled);
    root.setAttribute("contenteditable", options.disabled ? "false" : "true");

    if (entry.toolbar) {
      entry.toolbar.querySelectorAll("button[data-format]").forEach(function (button) {
        button.disabled = !!options.disabled;
      });
    }
  }

  function load(entry, html) {
    var quill = entry.quill;
    quill.setContents(quill.clipboard.convert({ html: html || "" }), "silent");
    if (quill.history && quill.history.clear) {
      quill.history.clear();
    }
    entry.lastEmitted = quill.root.innerHTML;
  }

  function emit(entry) {
    if (entry.timer) {
      clearTimeout(entry.timer);
      entry.timer = null;
    }
    if (entry.disposed || !entry.root) {
      return;
    }
    var html = entry.root.innerHTML;
    if (html === entry.lastEmitted) {
      return;
    }
    entry.lastEmitted = html;
    entry.reference.invokeMethodAsync("Changed", html).catch(function () { });
  }

  function schedule(entry) {
    if (entry.timer) {
      clearTimeout(entry.timer);
    }
    entry.timer = setTimeout(function () { emit(entry); }, pauseMilliseconds);
  }

  function toolbarButtons(entry) {
    return entry.toolbar ? Array.prototype.slice.call(entry.toolbar.querySelectorAll("button[data-format]")) : [];
  }

  // aria-pressed follows the formats at the caret, so the state is never colour alone.
  function refreshToolbar(entry) {
    if (!entry.toolbar || !entry.quill) {
      return;
    }
    var formats = {};
    try {
      formats = entry.quill.getSelection() ? entry.quill.getFormat() : {};
    } catch (error) {
      formats = {};
    }
    toolbarButtons(entry).forEach(function (button) {
      var format = button.getAttribute("data-format");
      var value = button.getAttribute("data-value");
      if (format === "clean") {
        return;
      }
      var pressed = value ? formats[format] === value : !!formats[format];
      button.setAttribute("aria-pressed", pressed ? "true" : "false");
    });
  }

  function requestLink(entry) {
    var quill = entry.quill;
    var range = quill.getSelection(true);
    var current = null;
    if (range) {
      var formats = quill.getFormat(range);
      current = typeof formats.link === "string" ? formats.link : null;
    }
    entry.reference.invokeMethodAsync("LinkRequested", current).then(function (url) {
      quill.focus();
      if (url === null || url === undefined) {
        return;
      }
      if (range) {
        quill.setSelection(range.index, range.length, "silent");
      }
      if (url === "") {
        quill.format("link", false, "user");
      } else if (range && range.length === 0) {
        // Nothing selected: the address becomes the text, so the link has something to hold on to.
        quill.insertText(range.index, url, { link: url }, "user");
      } else {
        quill.format("link", url, "user");
      }
      refreshToolbar(entry);
    }).catch(function () { });
  }

  function applyFormat(entry, button) {
    var quill = entry.quill;
    var format = button.getAttribute("data-format");
    var value = button.getAttribute("data-value");

    if (format === "link") {
      requestLink(entry);
      return;
    }

    quill.focus();
    var range = quill.getSelection();
    if (!range) {
      return;
    }
    if (format === "clean") {
      quill.removeFormat(range.index, range.length, "user");
    } else {
      var current = quill.getFormat();
      if (value) {
        quill.format(format, current[format] === value ? false : value, "user");
      } else {
        quill.format(format, !current[format], "user");
      }
    }
    refreshToolbar(entry);
  }

  // One tab stop for the toolbar: roving tabindex, Left and Right between the buttons, Home and End to the ends.
  function wireToolbar(entry) {
    var toolbar = entry.toolbar;
    if (!toolbar) {
      return;
    }

    function setRoving(target) {
      toolbarButtons(entry).forEach(function (button) {
        button.setAttribute("tabindex", button === target ? "0" : "-1");
      });
    }

    function onMouseDown(event) {
      // The editor keeps its selection: a click on a format must not take the focus away.
      if (event.target instanceof Element && event.target.closest("button[data-format]")) {
        event.preventDefault();
      }
    }

    function onClick(event) {
      var button = event.target instanceof Element ? event.target.closest("button[data-format]") : null;
      if (button && !button.disabled) {
        applyFormat(entry, button);
      }
    }

    function onFocusIn(event) {
      var button = event.target instanceof Element ? event.target.closest("button[data-format]") : null;
      if (button) {
        setRoving(button);
      }
    }

    function onKeyDown(event) {
      var buttons = toolbarButtons(entry).filter(function (button) { return !button.disabled; });
      var index = buttons.indexOf(document.activeElement);
      if (index < 0 || buttons.length === 0) {
        return;
      }
      var next = null;
      if (event.key === "ArrowRight") {
        next = buttons[(index + 1) % buttons.length];
      } else if (event.key === "ArrowLeft") {
        next = buttons[(index - 1 + buttons.length) % buttons.length];
      } else if (event.key === "Home") {
        next = buttons[0];
      } else if (event.key === "End") {
        next = buttons[buttons.length - 1];
      }
      if (next) {
        event.preventDefault();
        setRoving(next);
        next.focus();
      }
    }

    toolbar.addEventListener("mousedown", onMouseDown);
    toolbar.addEventListener("click", onClick);
    toolbar.addEventListener("focusin", onFocusIn);
    toolbar.addEventListener("keydown", onKeyDown);
    entry.cleanups.push(function () {
      toolbar.removeEventListener("mousedown", onMouseDown);
      toolbar.removeEventListener("click", onClick);
      toolbar.removeEventListener("focusin", onFocusIn);
      toolbar.removeEventListener("keydown", onKeyDown);
    });
  }

  function bind(entry, found) {
    var quill = found.quill;
    entry.quill = quill;
    entry.root = found.root;
    entry.toolbar = found.toolbar;
    entry.pending = false;

    // WCAG 2.1.2: Tab and Shift+Tab leave the editor, so the keyboard is never trapped in it.
    if (quill.keyboard && quill.keyboard.bindings) {
      delete quill.keyboard.bindings["Tab"];
      delete quill.keyboard.bindings[9];
    }

    // Only the kit's formats survive a paste; the Api sanitizes the rest again.
    if (quill.clipboard && quill.clipboard.addMatcher) {
      quill.clipboard.addMatcher(Node.ELEMENT_NODE, function (node, delta) {
        delta.ops.forEach(function (operation) {
          if (!operation.attributes) {
            return;
          }
          var kept = {};
          Object.keys(operation.attributes).forEach(function (name) {
            if (allowedFormats.indexOf(name) >= 0) {
              kept[name] = operation.attributes[name];
            }
          });
          operation.attributes = kept;
        });
        return delta;
      });
    }

    if (entry.options.html) {
      load(entry, entry.options.html);
    } else {
      entry.lastEmitted = found.root.innerHTML;
    }

    applyAttributes(entry);
    wireToolbar(entry);

    var onTextChange = function (delta, previous, source) {
      if (source === "user") {
        schedule(entry);
      }
    };
    var onEditorChange = function () { refreshToolbar(entry); };
    var onFocusOut = function () { emit(entry); };
    quill.on("text-change", onTextChange);
    quill.on("editor-change", onEditorChange);
    found.root.addEventListener("focusout", onFocusOut);
    entry.cleanups.push(function () {
      quill.off("text-change", onTextChange);
      quill.off("editor-change", onEditorChange);
      found.root.removeEventListener("focusout", onFocusOut);
    });

    if (entry.focusOnReady) {
      entry.focusOnReady = false;
      found.root.focus();
    }
  }

  return {
    // Resolves once the editable region is wired, or after a few seconds if the editor never came up (the field
    // stays usable: the Api still has the last value the page sent).
    attach: function (options, reference) {
      var entry = {
        options: options,
        reference: reference,
        pending: true,
        disposed: false,
        timer: null,
        focusOnReady: false,
        lastEmitted: "",
        cleanups: [],
        quill: null,
        root: null,
        toolbar: null
      };
      editors[options.id] = entry;

      return new Promise(function (resolve) {
        var attempts = 0;
        (function tryBind() {
          if (entry.disposed) {
            resolve();
            return;
          }
          var found = find(document.getElementById(options.hostId));
          if (found) {
            bind(entry, found);
            resolve();
            return;
          }
          attempts += 1;
          if (attempts > readyAttempts) {
            resolve();
            return;
          }
          setTimeout(tryBind, readyIntervalMilliseconds);
        })();
      });
    },

    update: function (options) {
      var entry = editors[options.id];
      if (!entry) {
        return;
      }
      var html = entry.options.html;
      entry.options = options;
      entry.options.html = html;
      if (!entry.pending) {
        applyAttributes(entry);
      }
    },

    setHtml: function (id, html) {
      var entry = editors[id];
      if (!entry) {
        return;
      }
      if (entry.pending) {
        entry.options.html = html;
        return;
      }
      load(entry, html);
    },

    focus: function (id) {
      var entry = editors[id];
      if (!entry) {
        return;
      }
      if (entry.pending) {
        entry.focusOnReady = true;
        return;
      }
      entry.root.focus();
    },

    detach: function (id) {
      var entry = editors[id];
      if (!entry) {
        return;
      }
      entry.disposed = true;
      if (entry.timer) {
        clearTimeout(entry.timer);
        entry.timer = null;
      }
      entry.cleanups.forEach(function (cleanup) { cleanup(); });
      delete editors[id];
    }
  };
})();
