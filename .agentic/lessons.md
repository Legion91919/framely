# Lessons

Generic, reusable lessons only. No client names, data or secrets.
- Give a module a port for what it needs from another module (here `IFollowerSource`) and implement it in the composition root; modules then need zero project references and the boundary test stays trivial.
- Generic `Assert.Equal` stand-ins can't infer types from collection expressions; compare two arrays.
- A stable keyset cursor needs a tie-breaker (a sequence number), not just a timestamp: bursts share timestamps.
