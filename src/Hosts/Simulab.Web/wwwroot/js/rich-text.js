// Rich text field (F-95). Plain script, loaded after quill.js and the Blazored.TextEditor adapter.
//
// Blazored.TextEditor creates the Quill instance inside the element the kit's AppRichTextField hosts it in. This
// file is the kit's side of the editor: it finds that instance and gives it the accessible name, the keyboard and
// the formats the item asks for. Nothing else in the app talks to Quill. The Api sanitizes whatever the editor
// holds; this script is comfort and accessibility, never the safety net.
window.simulabRichText = (function () {
  "use strict";

  // The formats the kit offers; they match the allow-list of the Api's sanitizer (F-95 BR8).
  // "alt" travels with a picture the dialog inserted (F-103).
  var allowedFormats = ["bold", "italic", "underline", "strike", "script", "list", "blockquote", "link", "alt"];
  // F-103 BR5, BR14: the one address a picture may have. A picture pasted or dropped from anywhere else is dropped here;
  // the Api removes it again on save.
  var storedImageAddress = /^\/question-images\/[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;
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

  // Quill drops an empty alt when it builds a picture, but an empty alt is how a decorative picture is marked (F-103 BR4).
  // The dialog always sets an alt, so a picture of the editor with none can only have lost an empty one.
  function keepDecorativeAlt(entry) {
    entry.root.querySelectorAll("img:not([alt])").forEach(function (image) {
      image.setAttribute("alt", "");
    });
  }

  function load(entry, html) {
    var quill = entry.quill;
    quill.setContents(quill.clipboard.convert({ html: html || "" }), "silent");
    if (quill.history && quill.history.clear) {
      quill.history.clear();
    }
    keepDecorativeAlt(entry);
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
    keepDecorativeAlt(entry);
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
      if (format === "clean" || format === "image" || format === "blank") {
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

  // F-103: the dialog stores the file and answers with its address and alternative text; the picture goes in at the caret.
  function requestImage(entry) {
    var quill = entry.quill;
    var range = quill.getSelection(true);
    entry.reference.invokeMethodAsync("ImageRequested").then(function (choice) {
      quill.focus();
      if (!choice || !storedImageAddress.test(choice.url)) {
        return;
      }
      var index = range ? range.index : quill.getLength();
      quill.insertEmbed(index, "image", choice.url, "user");
      // Set on the element itself: the editor's own format would drop an empty alt, the mark of a decorative picture.
      var leaf = quill.getLeaf(index)[0];
      if (leaf && leaf.domNode && leaf.domNode.setAttribute) {
        leaf.domNode.setAttribute("alt", choice.alt || "");
      }
      quill.setSelection(index + 1, 0, "silent");
      schedule(entry);
      refreshToolbar(entry);
    }).catch(function () { });
  }

  // F-107 BR3: the next blank marker is one more than the highest number the editor holds now (1 when none), read
  // from the editor's own text so a marker typed a moment ago counts. The pattern is BlankMarker.Pattern of the Api.
  // A marker goes up to 99: past it, the lowest free number is used, and with none free nothing is written (review #2).
  var blankNumberMax = 99;

  function nextBlankNumber(text) {
    var markers = /\[\[([1-9][0-9]?)\]\]/g;
    var used = {};
    var highest = 0;
    var match;
    while ((match = markers.exec(text)) !== null) {
      var number = parseInt(match[1], 10);
      used[number] = true;
      highest = Math.max(highest, number);
    }
    if (highest < blankNumberMax) {
      return highest + 1;
    }
    for (var candidate = 1; candidate <= blankNumberMax; candidate++) {
      if (!used[candidate]) {
        return candidate;
      }
    }
    return null;
  }

  function insertBlank(entry) {
    var quill = entry.quill;
    var range = quill.getSelection(true);
    var next = nextBlankNumber(quill.getText());
    if (next === null) {
      return;
    }
    var marker = "[[" + next + "]]";
    var index = range ? range.index : Math.max(quill.getLength() - 1, 0);
    if (range && range.length > 0) {
      quill.deleteText(range.index, range.length, "user");
    }
    quill.insertText(index, marker, "user");
    quill.setSelection(index + marker.length, 0, "silent");
    schedule(entry);
    refreshToolbar(entry);
  }

  function applyFormat(entry, button) {
    var quill = entry.quill;
    var format = button.getAttribute("data-format");
    var value = button.getAttribute("data-value");

    if (format === "link") {
      requestLink(entry);
      return;
    }

    if (format === "image") {
      requestImage(entry);
      return;
    }

    if (format === "blank") {
      insertBlank(entry);
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

    // F-103 BR14: only the dialog inserts a picture. The editor's uploader would turn a pasted or dropped file into a
    // data: picture, so it does nothing, and a drop of files is stopped before the editor sees it.
    var uploader = quill.getModule ? quill.getModule("uploader") : null;
    if (uploader && uploader.options) {
      uploader.options.handler = function () { };
    }
    var stopFileDrop = function (event) {
      if (event.dataTransfer && event.dataTransfer.files && event.dataTransfer.files.length > 0) {
        event.preventDefault();
        event.stopPropagation();
      }
    };
    quill.container.addEventListener("drop", stopFileDrop, true);
    quill.container.addEventListener("dragover", stopFileDrop, true);
    entry.cleanups.push(function () {
      quill.container.removeEventListener("drop", stopFileDrop, true);
      quill.container.removeEventListener("dragover", stopFileDrop, true);
    });

    // Only the kit's formats survive a paste; the Api sanitizes the rest again. A picture survives only with the address
    // of a stored one (a stored text is loaded through this same matcher).
    if (quill.clipboard && quill.clipboard.addMatcher) {
      quill.clipboard.addMatcher(Node.ELEMENT_NODE, function (node, delta) {
        delta.ops = delta.ops.filter(function (operation) {
          var image = operation.insert && typeof operation.insert === "object" ? operation.insert.image : null;
          return !image || storedImageAddress.test(image);
        });
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
