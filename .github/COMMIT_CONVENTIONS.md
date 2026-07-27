# OCAP Commit Conventions

All commits in this repository MUST follow the Conventional Commits specification.

Format:
```
type(scope): short description

- Detailed change.
- Detailed change.
- Detailed change.

Impact:
- Explain technical impact.
```

## Allowed Types

- `feat`: A new feature
- `fix`: A bug fix
- `docs`: Documentation only changes
- `refactor`: A code change that neither fixes a bug nor adds a feature
- `test`: Adding missing tests or correcting existing tests
- `chore`: Changes to the build process or auxiliary tools and libraries such as documentation generation
- `ci`: Changes to our CI configuration files and scripts
- `build`: Changes that affect the build system or external dependencies

## Not Allowed

Never use titles like:
- `update`
- `changes`
- `stuff`
- `progress`
- `fixed bug`
