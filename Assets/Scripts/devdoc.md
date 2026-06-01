# Dev Doc

This system is heavily inspired by the Lichtenauer longsword tradition. There are no attacks in a traditional sense, instead
you pass through guards, and depending on distance and timing a technique is executed.

The goal is to create an ARPG combat system that captures the feeling of real longsword fencing without it being a simulation.
The System is hoped to create emergent gameplay where as much as possible results are not scripted. The System shall preferably
only consist of:

- Guards as static poses
- Transitions between guards, with intermediate poses to fix ugliness
- Timers (parrying windows, critical strikes, Vor/Indes)
- Velocity multipliers for damage (weapon tip and motion)
- Distance checks for Zufechten/Krieg (possibly)
  - Later version may utilise SDF for precise distances -> one method for checking hit and penetration depth.
- Parrying tables* (Which guard/transition breaks what?)



*Parrying tables:
Not every transition will block an incoming attack. Meeting a Zornhau (VomTag → Langort → Alber (or Pflug)) with a 
Mittelhau (e.g. Pflug R → Pflug L) will not work. I guess this is where simulation gets really fucking tempting -.^
But the plan is to have tables with relations (a graph I guess?) for which opposing combinations cancel out.
(An option is directional checks I guess. Requires some thought maybe.)

### Distance/Timing

#### Zufechten

The measure where 99% of fencing exists (in real life, at least). I guess this is kinda where the System is Make or Break.
Utility AI could be an option to use to get AI to be hesitant to attack against certain guards at range etc.

"Zufechten (out of measure):
At this range, only certain transitions should be allowed. You can define a minDistance and maxDistance per transition. 
If the player is too far, the transition either doesn't start or becomes a "feint" (starts but auto-cancels after 0.2s). 
This encourages players to close distance."

#### Krieg

"Beware, here there be swords!" aka, "Move fast or die!"


"Krieg (close measure):
Here, transitions complete faster (reduce blendDuration dynamically), and parry windows shrink. You could also automatically 
force certain "binding" transitions (e.g., from Ochs to Langort when enemy is too close) – that emulates the winding."

#### Ringen am Scwhert

This distance is not one that I will aim to do anything with. Doing so would be to commit to a whole wrestling system, and 
I do not wish to do so. At most, there may be a Shove reaction or a trigger when stepping in with good timing to Trip the opponent. 
Not more than such.

#### Vor/Indes

I am thinking about something along the lines of small adjustments to blend time; shorter while having the initiative and longer when not. 
This may already be enough to simulate vor/indes (with animations by default not being interruptable).




TODO:

- Targeting (+ target dummy)
- Damage/Health
- Side-stepping (to get out of line and line up attacks)
- Timer system

