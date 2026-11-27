---
name: github-issue-status-labels
description: Set up or migrate a GitHub repository so that "status:" labels mirror GitHub's issue statuses (Backlog, Todo, In progress, In review, Done, Not planned, Duplicate), kept in sync by a GitHub Actions workflow. Use this whenever the user wants status labels, wants to filter the issue list by status, wants to automate issue status or labels, replace labels such as "todo", "in progress", "needs triage" or "wontfix", roll out the Platform Kite issue workflow, or debug a status label that does not update.
---

# GitHub issue status labels

Mirrors GitHub's issue status in one `status: <name>` label per issue, so the issue list can be
filtered by status through the label filter.

The **status is the source of truth** and labels only follow it. To move an issue, change its status;
a `status:` label that is set by hand is overwritten at the next status change. Issue statuses are in
public preview, so GitHub may change their names, API or automatic rules.

## Who moves the status

| Event | New status | Moved by |
| --- | --- | --- |
| Issue opened or reopened | Backlog | GitHub |
| Person assigned | Todo | GitHub |
| Branch such as `56-add-x` or `feat/56-add-x` created | In progress | The workflow |
| Draft pull request linked | In progress | GitHub |
| Pull request ready for review | In review | GitHub |
| Pull request closed unmerged or back to draft | In progress | GitHub |
| Issue closed | Done, Not planned or Duplicate | GitHub |

A pull request is linked by a closing keyword such as `Closes #56` (the pull request must target the
default branch) or from the Development section of the issue. A pull request that is opened ready for
review moves the issue straight to In review.

## Files to install

The files keep their names, so that a repository is easy to compare with the skill.

| Skill file | Destination in the repository |
| --- | --- |
| `assets/issue-status-labels.yml` | `.github/workflows/issue-status-labels.yml` |
| `assets/github-labels.jsonc` | `.github/github-labels.jsonc` |

## How to work

Changing labels and statuses affects every issue and everyone who watches the repository, and a
deleted label disappears from all its issues without an undo. So first inspect, then show the user a
plan with the exact labels to rename, create and delete and the issue statuses to set, and get
approval before changing anything on GitHub. Ask again before deleting. Follow the steps in order:
each protects the next.

### 1. Check and take inventory

Confirm that `gh auth status` succeeds and which repository you are working on
(`gh repo view --json nameWithOwner`). Then check that the repository has issue statuses:

```bash
gh api graphql -F owner='{owner}' -F repo='{repo}' -f query='query($owner:String!,$repo:String!){repository(owner:$owner,name:$repo){planningStatuses(first:10){nodes{name}}}}'
```

An error or an empty list means the preview is not available for this repository. Stop and tell the
user; without statuses the workflow has nothing to mirror.

Collect:

- the labels (`gh label list --limit 500`)
- workflows that add or remove labels (search `.github/workflows` for `addLabels`, `removeLabel`,
  `--add-label` and labeler actions)
- other places that name labels, such as issue forms in `.github/ISSUE_TEMPLATE`, search links in
  documentation, and tool configuration

Labels that a tool manages, such as `autorelease: pending` and `autorelease: tagged` from
release-please or the labels Dependabot adds, stay untouched. Releases break without them.

### 2. Back up

Save both outside the repository, so that labels can be restored:

```bash
gh label list --limit 500 --json name,color,description > <backup-folder>/labels.json
gh api "repos/{owner}/{repo}/issues?state=all&per_page=100" --paginate --jq '.[] | {number, state, state_reason, status: .status.name, pull_request: (.pull_request != null), labels: [.labels[].name]}' > <backup-folder>/issues.jsonl
```

### 3. Change the repository files

1. Copy the two files from the table above. If a destination exists, compare and merge instead of
   overwriting.
2. In `.github/github-labels.jsonc`, adjust the type labels to the repository and add the labels it keeps.
   Leave the seven `status:` names as they are: the workflow builds the name from the status.
3. Remove other workflows or steps that set status labels. Left in place they recreate deleted
   labels, because adding an unknown label to an issue creates it.
4. Update issue forms and other references to the new label names. Forms need type labels only; the
   workflow adds the status label.

Leave the changes for the user to review and follow the repository's commit and pull request
conventions. Continue after the workflow is on the default branch, since GitHub runs issue workflows
from that branch only.

### 4. Rename existing type labels

Renaming keeps a label on its issues; deleting and recreating does not. Rename every existing label
that has a counterpart in the new file, for example:

```bash
gh label edit "feature" --name "enhancement"
```

If the new name already exists, the rename fails. Add the new label to each issue and pull request
that has the old one (`gh issue list` and `gh pr list` with `--label "<old>" --state all --limit 1000`,
then `gh issue edit` or `gh pr edit` with `--add-label`), and delete the old label in step 7.

### 5. Create the labels

With the Platform Kite module installed, preview with `-WhatIf` first:

```powershell
Set-GitHubLabels -LabelFilePath .github/github-labels.jsonc -Owner <owner> -Repository <repo> -KeepExistingLabels
```

Without the module, run `gh label create "<name>" --color <hex without #> --description "<text>" --force`
for each label in the file. Neither route deletes labels.

### 6. Give every issue a status, then sync

The workflow leaves issues without a status alone, which includes most issues created before statuses
existed. An open issue can also show Backlog although an old label says it is further along, and
step 7 deletes that label. So set a status for these issues only:

- issues without a status
- open issues in Backlog that match the In review, In progress or Todo row below

Never change any other status. An old label can be out of date, so list each change in the plan. Take
the first row that matches:

| Issue | Status |
| --- | --- |
| Closed as completed, or without a close reason | Done |
| Closed as not planned | Not planned |
| Closed as duplicate | Duplicate |
| Open, with a linked pull request ready for review or an "in review" label | In review |
| Open, with an "in progress" label | In progress |
| Open, with a "todo" label | Todo |
| Any other open issue | Backlog |

```bash
gh api -X PATCH "repos/{owner}/{repo}/issues/<number>" -f status="Done"
```

Duplicate also needs `-F duplicate_issue_id=<id>`, the `id` of the original issue
(`gh api "repos/{owner}/{repo}/issues/<original>" --jq .id`), not its number. Ask the user when the
original is unclear.

Then add the labels to all issues and check that the run succeeds:

```bash
gh workflow run issue-status-labels.yml
gh run list --workflow issue-status-labels.yml --limit 1
```

### 7. Delete the replaced labels

After the status labels are verified, list the labels to delete and ask for approval. Typical
candidates are `todo`, `in progress`, `in review`, `needs triage`, `completed`, `wontfix`, `invalid`
and `duplicate`.

```bash
gh label delete "<name>" --yes
```

### 8. Verify

With a test issue, check that the label follows each change: open it, assign it, create a branch for
it, link a draft pull request, mark that ready for review, close the pull request, close the issue.
Report what you saw for each step. Close the test pull request unmerged and delete the test branch.

## Troubleshooting

- **No label changes:** the workflow must be on the default branch, and the issue needs a status.
- **Branch did not move the issue:** the name must start with the issue number, optionally after one
  prefix folder. Only open issues in Backlog or Todo move.
- **A status changed but the label did not:** GitHub does not guarantee delivery of status events.
  Run `gh workflow run issue-status-labels.yml` to resync every issue.
- **Pull request did not move the issue:** check that it is linked and targets the default branch.
