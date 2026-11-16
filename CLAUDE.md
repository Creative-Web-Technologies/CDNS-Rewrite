## Protected: database, migrations and seed data

These rules apply to every task, including verification, and override any
instruction to "verify on a fresh database" or "confirm against seeded rows".

1. Never drop, reset, truncate, re-create or re-migrate the cdns_dev database, and
   never run `dotnet ef database drop` or `dotnet ef database update` against it.
   Do not start the API in a way that runs seeding against cdns_dev unless I ask.
2. Never insert, update or delete rows in cdns_dev directly (psql, scripts, EF, or
   any other route). Reading with SELECT is allowed.
3. Never edit, rename, delete or regenerate any existing file under
   backend/src/Cdns.Infrastructure/Migrations/, including the model snapshot.
   Never add a new migration unless I explicitly ask for one in that task.
4. Never edit or delete existing seed data files (AuthSeedData,
   MemberPortalSeedData, or any later seed class) and never change what they
   insert, unless I explicitly ask for a seed change in that task.
5. If a task seems to need any of the above, stop and ask me first. Explain what
   you need and why, and wait for my answer.
6. For verification that needs a database, create a separate throwaway database
   with a different name, point only your own test run at it, and drop only that
   throwaway database afterwards. Tell me its name in your report.
7. Never install Playwright, Puppeteer or any browser automation, and never
   download a browser. Verify UI work with build, lint and API calls only.
8. Never commit, push, or change branches unless I ask.
