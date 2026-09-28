#!/usr/bin/env bash
# Records and checks which content of a deliverable (e.g. 'iac' or 'src') was deployed to staging successfully,
# so a release only reaches production with content that staging has seen.
#
#   stg-verification.sh mark   <folder> <status-context>                 after a successful staging deployment
#   stg-verification.sh verify <folder> <status-context> <release-ref>   before a production deployment
#
# 'mark' sets a commit status (<status-context>, e.g. 'kite/stg-infra') on the deployed commit. 'verify' looks on
# main for a commit whose deliverable content is identical to the release and that carries a successful status.
# Content is compared, not commit position: a release commit that only updates the changelog or version file
# (as a release PR does) passes, and a commit whose content failed in staging does not.
#
# Requires GH_TOKEN (statuses: write for 'mark', statuses: read for 'verify'), GITHUB_REPOSITORY, and for 'verify' a
# full-history checkout (fetch-depth: 0) that includes origin/main.
set -euo pipefail

command="${1:?usage: stg-verification.sh mark|verify <folder> <status-context> [<release-ref>]}"
folder="${2:?missing <folder>}"
context="${3:?missing <status-context>}"

# Release bookkeeping files inside <folder>; changing them doesn't change what gets deployed
ignored_files="CHANGELOG.md|version.txt"

# Hash of every file under <folder> (path + blob), except the release bookkeeping files
fingerprint() {
  git ls-tree -r "$1" -- "$folder" \
    | { grep -v -E $'\t'"${folder}/(${ignored_files//./\\.})\$" || true; } \
    | git hash-object --stdin
}

case "$command" in
  mark)
    sha="$(git rev-parse HEAD)"
    content="$(fingerprint "$sha")"
    gh api --silent -X POST "repos/${GITHUB_REPOSITORY}/statuses/${sha}" \
      -f state=success \
      -f context="$context" \
      -f description="Deployed to staging (content ${content:0:12})" \
      -f target_url="${GITHUB_SERVER_URL:-https://github.com}/${GITHUB_REPOSITORY}/actions/runs/${GITHUB_RUN_ID:-0}"
    echo "Marked ${sha} as deployed to staging ('${context}', content ${content})."
    ;;

  verify)
    release_ref="${4:?missing <release-ref>}"
    release_sha="$(git rev-parse "${release_ref}^{commit}")"
    release_content="$(fingerprint "$release_sha")"

    if ! git merge-base --is-ancestor "$release_sha" origin/main; then
      echo "::error::Release '${release_ref}' (${release_sha}) is not on main. Cut releases from main."
      exit 1
    fi

    # The last commit up to the release that changed deployable content: any staging deployment of this
    # content ran on that commit or on a later one
    content_sha="$(git log -1 --format=%H "$release_sha" -- "$folder" ":(exclude)${folder}/CHANGELOG.md" ":(exclude)${folder}/version.txt")"
    if [ -z "$content_sha" ]; then
      echo "::error::Release '${release_ref}' contains no '${folder}' content to deploy."
      exit 1
    fi

    # That commit plus its descendants on main, newest first; staging runs at most once per push, so a few suffice
    candidates="$( { git rev-list --ancestry-path "${content_sha}..origin/main"; echo "$content_sha"; } | head -n 200)"
    for candidate in $candidates; do
      [ "$(fingerprint "$candidate")" = "$release_content" ] || continue

      state="$(gh api "repos/${GITHUB_REPOSITORY}/commits/${candidate}/statuses?per_page=100" \
        --jq "[.[] | select(.context == \"${context}\")][0].state // \"\"")"
      if [ "$state" = "success" ]; then
        echo "Release '${release_ref}' (${release_sha}) matches content deployed to staging by ${candidate} ('${context}', content ${release_content})."
        exit 0
      fi
    done

    echo "::error::The '${folder}' content of release '${release_ref}' (${release_content:0:12}) has no successful staging deployment ('${context}'). Merge it to main, let CD deploy it to stg, then release again."
    exit 1
    ;;

  *)
    echo "::error::Unknown command '${command}'; expected 'mark' or 'verify'."
    exit 2
    ;;
esac
