#!/usr/bin/env bash
#
# Regenerates the gh-pages landing page: one card per live PR preview.
#
# Run with the current directory set to the root of a gh-pages checkout. It
# scans pr-preview/pr-<n>/ directories, so it always reflects what is actually
# deployed -- both the deploy job and the cleanup job call it after they have
# added or removed a directory, and neither has to track state anywhere else.
#
# PR titles come from `gh pr view`, which needs GH_TOKEN. If the lookup fails
# (deleted PR, rate limit) the card falls back to the bare PR number rather
# than failing the run -- a slightly plainer landing page beats a red build.
set -euo pipefail

REPO="${REPO:?REPO must be set (owner/name)}"
OUT="index.html"

html_escape() {
  sed -e 's/&/\&amp;/g' -e 's/</\&lt;/g' -e 's/>/\&gt;/g' -e 's/"/\&quot;/g'
}

# Newest PR first, so the thing you just pushed is at the top.
mapfile -t PR_NUMBERS < <(
  find pr-preview -mindepth 1 -maxdepth 1 -type d -name 'pr-*' -printf '%f\n' 2>/dev/null |
    sed 's/^pr-//' | grep -E '^[0-9]+$' | sort -rn || true
)

cards=""
for num in "${PR_NUMBERS[@]:-}"; do
  [ -n "$num" ] || continue
  title=$(gh pr view "$num" --repo "$REPO" --json title --jq .title 2>/dev/null || true)
  [ -n "$title" ] || title="Pull request #$num"
  title=$(printf '%s' "$title" | html_escape)
  cards+="      <li class=\"card\">
        <a class=\"card-link\" href=\"pr-preview/pr-${num}/\">
          <span class=\"pr-number\">#${num}</span>
          <span class=\"pr-title\">${title}</span>
        </a>
        <a class=\"pr-link\" href=\"https://github.com/${REPO}/pull/${num}\">view pull request &rarr;</a>
      </li>
"
done

if [ -z "$cards" ]; then
  cards="      <li class=\"empty\">No open pull request previews right now.</li>
"
fi

cat > "$OUT" <<HTML
<!DOCTYPE html>
<html lang="en">

<head>
  <meta charset="utf-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1.0" />
  <title>Catandomizer PR previews</title>
  <style>
    :root { color-scheme: light dark; }
    body {
      margin: 0;
      padding: 2rem 1rem 3rem;
      font-family: system-ui, -apple-system, "Segoe UI", sans-serif;
      line-height: 1.5;
      background: #f6f4ef;
      color: #23201b;
    }
    main { max-width: 40rem; margin: 0 auto; }
    h1 { margin: 0 0 .25rem; font-size: 1.75rem; }
    .subtitle { margin: 0 0 2rem; color: #6b635a; }
    ul { list-style: none; margin: 0; padding: 0; display: grid; gap: .75rem; }
    .card {
      background: #fff;
      border: 1px solid #e0d9cd;
      border-radius: .5rem;
      padding: .9rem 1.1rem;
    }
    .card-link { display: block; text-decoration: none; color: inherit; }
    .card-link:hover .pr-title { text-decoration: underline; }
    .pr-number { font-weight: 700; color: #a4652a; margin-right: .5rem; }
    .pr-title { font-weight: 600; }
    .pr-link { display: inline-block; margin-top: .35rem; font-size: .85rem; color: #6b635a; }
    .empty { color: #6b635a; font-style: italic; }
    footer { margin-top: 2.5rem; font-size: .85rem; color: #6b635a; }
    a { color: #a4652a; }
    @media (prefers-color-scheme: dark) {
      body { background: #1b1917; color: #ece7df; }
      .subtitle, .pr-link, .empty, footer { color: #a9a096; }
      .card { background: #262320; border-color: #3a3530; }
      .pr-number, a { color: #e0a260; }
    }
  </style>
</head>

<body>
  <main>
    <h1>Catandomizer PR previews</h1>
    <p class="subtitle">Live builds of every open pull request, so a change can be played with before it merges.</p>
    <ul>
${cards}    </ul>
    <footer>
      Previews call the production board service at
      <a href="https://catandomizerservice.azurewebsites.net">catandomizerservice.azurewebsites.net</a>,
      so service-side changes in a pull request are not reflected here.
      Each preview is removed when its pull request closes.
      <br />
      <a href="https://catandomizer.azurewebsites.net">Production site</a> &middot;
      <a href="https://github.com/${REPO}">Repository</a>
    </footer>
  </main>
</body>

</html>
HTML

echo "Wrote $OUT with ${#PR_NUMBERS[@]} preview(s)"
