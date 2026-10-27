# Release notes

Each file in this folder is the Markdown body of one GitHub Release.

- Name the file after the version without the leading `v`: tag `v2.0.1` reads `2.0.1.md`.
- Write normal GitHub Markdown: headings, lists, tables, links, code and `> [!NOTE]` alerts all render on the release page.
- When the tag is pushed, the release workflow appends download instructions and the SHA256 table for the built packages. Do not add them here.
- If no file exists for a tag, the release links to the changelog in the main README instead.
