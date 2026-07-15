# Roadmap Rule
Always follow the specific micro-versions defined in the `Roadmap hy.md` file located at the project root (`C:\Users\yoruk\.gemini\antigravity\scratch\TrainService\Roadmap hy.md`). Do not skip phases or assign version numbers arbitrarily. Read the roadmap carefully to understand the exact feature requested for each `v3.0.X` iteration.

If the agent is unsure about the current version, it MUST check `Roadmap hy.md` first.

# Superpowers Rule
You MUST adhere to the instructions, prompt structures, and behavioral guidelines defined in the `obra/superpowers` repository (https://github.com/obra/superpowers). Emulate an expert-level (10x) software engineer focusing on precision, architectural purity, efficiency, and robust problem-solving.

# Strict Workflow Rules
You MUST strictly follow this sequence for any feature development:
1. **Pre-Plan Check:** ALWAYS read the `Roadmap hy.md` to identify exactly where we are and what is next.
2. **Rule Verification:** Re-read this `AGENTS.md` file to refresh Superpowers and strict rules before planning.
3. **Planning:** Create an `implementation_plan.md` detailing the upcoming changes and STOP. Request explicit user approval.
4. **Execution:** ONLY proceed with file modifications after the user approves the plan.
5. **App Verification:** Once implementation is complete, run the application (`dotnet run`) and STOP. Ask the user to manually test the changes on their screen.
6. **Finalizing & Push:** ONLY AFTER the user confirms everything works and explicitly says "pushla" (push), write a detailed walkthrough/README and push the code to Git. DO NOT push before user confirmation.
