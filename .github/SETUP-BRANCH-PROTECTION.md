# Quick Setup: Branch Protection for Master Branch

This guide will help you quickly enable branch protection on the `master` branch to require builds to pass before merging.

## Prerequisites

- You must have **admin** access to the repository
- The `.NET` GitHub Actions workflow must be enabled (it should run automatically on PRs)

## Setup Steps (5 minutes)

### Option 1: GitHub Web Interface (Recommended)

1. **Navigate to Settings**
   - Go to https://github.com/splusq/follower
   - Click **Settings** (top right)
   - Click **Branches** (left sidebar)

2. **Add Protection Rule**
   - Click **Add branch protection rule**
   
3. **Configure the Rule**
   - **Branch name pattern:** Enter `master`
   - **Protect matching branches:** Check these boxes:
     - ✅ **Require status checks to pass before merging**
     - ✅ **Require branches to be up to date before merging**
     - In the status checks search box, type `build` and select it
       - Note: You may need to create a test PR first for the `build` status to appear
   
4. **Additional Settings (Optional but Recommended)**
   - ❌ Leave **Allow force pushes** unchecked
   - ❌ Leave **Allow deletions** unchecked
   - ✅ Consider checking **Require a pull request before merging** (if you want code reviews)

5. **Save**
   - Scroll down and click **Create** or **Save changes**

### Option 2: GitHub CLI

If you have the GitHub CLI installed:

```bash
# Install gh if needed: https://cli.github.com/

# Login
gh auth login

# Enable branch protection with required status checks
gh api repos/splusq/follower/branches/master/protection \
  --method PUT \
  --field required_status_checks='{"strict":true,"contexts":["build"]}' \
  --field enforce_admins=false \
  --field required_pull_request_reviews=null \
  --field restrictions=null
```

### Option 3: Terraform (For Infrastructure as Code)

If you manage GitHub with Terraform:

```hcl
resource "github_branch_protection" "master" {
  repository_id = "follower"
  pattern       = "master"

  required_status_checks {
    strict   = true
    contexts = ["build"]
  }

  enforce_admins = false
}
```

## Verification

After setting up branch protection:

1. **Create a test PR:**
   ```bash
   git checkout -b test-branch-protection
   echo "test" >> README.md
   git add README.md
   git commit -m "Test branch protection"
   git push origin test-branch-protection
   ```

2. **Open a PR on GitHub** targeting `master`

3. **Verify the checks:**
   - You should see a "build" status check running
   - The PR should show "Merging is blocked" until the check passes
   - After the check passes, the "Merge pull request" button should become available

4. **Clean up:**
   ```bash
   git checkout master
   git branch -D test-branch-protection
   git push origin --delete test-branch-protection
   ```

## Troubleshooting

### "No status checks found"

If the `build` status check doesn't appear in the list:
1. Create a test PR first (see Verification above)
2. Wait for the GitHub Actions workflow to run
3. After the workflow runs, the `build` status will appear in the branch protection settings

### "Unable to merge" even when checks pass

- Ensure "Require branches to be up to date before merging" is checked
- Update your branch with the latest from `master`:
  ```bash
  git checkout your-branch
  git pull origin master
  git push
  ```

### Workflow not running

Check that:
- The workflow file is present: `.github/workflows/dotnet.yml`
- GitHub Actions is enabled for the repository (Settings → Actions)
- The workflow has permission to run (Settings → Actions → General)

## What Happens After Setup?

Once branch protection is enabled:

✅ **Pull Requests:** All PRs to `master` must have passing checks before merging
✅ **Direct Pushes:** Prevented (all changes must go through PRs)
✅ **Quality Assurance:** No broken builds can be merged to `master`

## Need Help?

- See [`CONTRIBUTING.md`](../CONTRIBUTING.md) for detailed contribution guidelines
- See [`.github/branch-protection.yml`](branch-protection.yml) for the complete configuration reference
- GitHub Docs: https://docs.github.com/en/repositories/configuring-branches-and-merges-in-your-repository/managing-protected-branches

---

**Estimated setup time:** 5 minutes  
**Impact:** High (prevents broken builds on master)  
**Maintenance:** None (set once, works forever)
