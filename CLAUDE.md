# CLAUDE.md - Development Guidelines
This is the LLM oriented guidelines. Not a readme. This is more a prompt/context.
Don't put unnecessary common things in here

## On startup, follow these instructions :
**Load all md files you can find.**: On startup, load everything
**Don't run code directly**: The Python venv is Windows-based while Claude runs on WSL - running tests directly will fail.
**Update documentation**: Every time a new feature is implemented, update the relevant documentation in this file, README.md, and TECH_DETAILS.md to reflect the changes.
**Keep code and documentation in sync**: Ensure schema definitions, function signatures, and documentation stay consistent.
**Don't maintain backward compatibility**: When implementing new features or fixing issues, prioritize correctness and adherence to specifications over backward compatibility. It's better to break existing code and fix it properly than to maintain compatibility with incorrect implementations.
**Rely on dependency injection pattern**: For instance, do not create global static function such as get_manager, get_client or things like that. 
**Don't create singleton**: Again, prefer using dependency injection pattern whenever possible. Production ready code.
**Always think of writing unit test**: Everything should be heavily tested.
**Use object oriented**: Don't create function in the middle of nowhere 