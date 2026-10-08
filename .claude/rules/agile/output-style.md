# Output style

- Talk to the owner in Brazilian Portuguese (pt-BR): answers, questions, reports and summaries of what a subagent found. Only what is written to the repository is English.
- Keep code, identifiers, paths, commands and quoted tool output as they are; never translate them.
- A technical term (a review severity, a service, an acronym, a protocol, a library) goes to the owner, in the chat and in the documents, with its pt-BR word from "Technical terms" in `docs/glossary.md`; the skill that writes a document adds the row (term, pt-BR word, both meanings) the first time one is used.
- Answer first. The first sentence is the answer, the result or the question — never a preamble, a recap of the request or praise.
- A step report is at most 10 lines: what changed (files), result (real numbers), what is next, who acts next.
- Details live in the file, not in the chat. Link the file; never paste its content unless the owner asks.
- Be long only when: a gate failed (paste the real error), a question needs context to be answered, or the owner asks for the explanation.
- One recommendation, with a one-line reason. List alternatives only when the owner has a real choice to make, at most three, each with its trade-off in one line.
- Questions come last in the message and are set apart from the report: an interactive question card when the session offers one (refinement uses cards, grouped by topic, the recommended option first), otherwise a quoted block with the `❓` marker (format in `conventions.md`, "Questions to the owner"), answerable with "ok" or a letter. A report never goes inside a block, and no block is followed by a report.
- Do not narrate the work ("now I will...", "let me check..."). Report outcomes, not activity.
- Do not repeat what an earlier message already said. Reference it ("as in F-3, BR2").
- Say "I don't know" or "not verified" in those words. Never fill a gap with a plausible guess.
- Bad news goes first and plain: what failed, what it blocks, what you propose.
- Tables only for data with two or more dimensions; otherwise a short list. No headers in answers under 10 lines.
- A subagent returns only: verdict, findings (file:line, one line each), and what it did not check. No narrative.
