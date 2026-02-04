# Contributing to Follower

Thank you for your interest in contributing to Follower! This document provides guidelines for contributing to the project.

## Branch Protection Requirements

The `master` branch is protected to ensure code quality and prevent broken builds. Before code can be merged to `master`, all required status checks must pass.

### Required Status Checks

The following GitHub Actions workflow must complete successfully:

- **Build Job** (`.NET` workflow): Compiles the code, restores dependencies, and runs all tests

### Setting Up Branch Protection (For Repository Administrators)

Branch protection rules must be configured in the GitHub repository settings. Follow these steps:

#### Via GitHub Web Interface

1. Navigate to the repository on GitHub
2. Go to **Settings** → **Branches**
3. Click **Add branch protection rule** (or edit existing rule for `master`)
4. Configure the following settings:

   **Branch name pattern:** `master`

   **Protect matching branches:**
   - ✅ Check **"Require status checks to pass before merging"**
   - ✅ Check **"Require branches to be up to date before merging"**
   - In the search box, find and select: **`build`**
   - ✅ Check **"Do not allow bypassing the above settings"** (optional, recommended for strict enforcement)
   - ❌ Uncheck **"Allow force pushes"**
   - ❌ Uncheck **"Allow deletions"**

5. Click **Create** or **Save changes**

#### Via GitHub CLI

If you have the GitHub CLI (`gh`) installed and authenticated:

```bash
# Enable branch protection with required status checks
gh api repos/splusq/follower/branches/master/protection \
  --method PUT \
  --field required_status_checks='{"strict":true,"contexts":["build"]}' \
  --field enforce_admins=false \
  --field required_pull_request_reviews=null \
  --field restrictions=null
```

#### Via GitHub API

You can also use the GitHub REST API directly. See the [Branch Protection API documentation](https://docs.github.com/en/rest/branches/branch-protection) for details.

### Configuration Reference

The complete branch protection configuration is documented in [`.github/branch-protection.yml`](.github/branch-protection.yml).

## Development Workflow

### 1. Fork and Clone

```bash
git clone https://github.com/splusq/follower.git
cd follower
```

### 2. Create a Feature Branch

```bash
git checkout -b feature/your-feature-name
```

### 3. Make Changes

Edit the code, following the existing code style and conventions.

### 4. Build and Test Locally

Before submitting a PR, ensure all checks pass locally:

```bash
# Restore dependencies
dotnet restore

# Build the project
dotnet build

# Run tests
dotnet test
```

Or use the Makefile shortcuts:

```bash
make restore
make build
make test
```

### 5. Commit Your Changes

Write clear, descriptive commit messages:

```bash
git add .
git commit -m "Add feature: description of what was added"
```

### 6. Push to Your Fork

```bash
git push origin feature/your-feature-name
```

### 7. Create a Pull Request

1. Go to the repository on GitHub
2. Click **"Pull requests"** → **"New pull request"**
3. Select your feature branch
4. Fill in the PR template with:
   - Description of changes
   - Testing performed
   - Any related issues
5. Submit the PR

### 8. Wait for CI Checks

GitHub Actions will automatically run the build and tests on your PR. You can view the status on the PR page.

**The PR cannot be merged until:**
- ✅ The `build` job passes
- ✅ All tests pass
- ✅ The branch is up to date with `master`

### 9. Address Feedback

If the checks fail or reviewers request changes:

1. Make the necessary updates on your branch
2. Commit and push the changes
3. The CI checks will automatically re-run

### 10. Merge

Once all checks pass and any required reviews are complete, the PR can be merged to `master`.

## Code Style

- Follow standard C# conventions
- Use meaningful variable and method names
- Add comments for complex logic
- Keep methods focused and concise
- Write unit tests for new functionality

## Testing

- All new features should include unit tests
- Ensure existing tests continue to pass
- Aim for high test coverage of critical paths

## Questions?

If you have questions about contributing, please:
- Open an issue on GitHub
- Check existing issues and pull requests for similar discussions

Thank you for contributing! 🎉
