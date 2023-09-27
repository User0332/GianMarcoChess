# GianMarco

A work-in-progress UCI-compatible chess engine written in C# utilizing a search-and-eval strategy. This project uses modified code from Sebastian Lague's `ChessChallenge.Chess` and `ChessChallenge.API` namespaces. Play the bot on lichess ([@GianMarcoChessBot](https://lichess.org/@/GianMarcoChessBot))!

## Todo
- Add more pruning techniques for increased depth
- Look into why initial evals comes out to ~275 cp - IMPORTANT
- Fix bugs in move line generation - IMPORTANT
- Fix endgme checkmates (problem found w/ king & queen vs. king -- finds forced checkmate sequence but loses the sequence on the next few moves)
- Add pondering
- Maybe - add bias against draw (by modifying (`GianMarco.Evaluation.Constants.DrawValue`))
- Add eval that encourages bot to develop pieces
- Add eval for rooks
	- Rooks on open files
	- Doubled rooks (and queens)
	- Increase rook weight in endgame
- Maybe add specialized endgame puzzle evals (e.g. if its endgame and its rook & king vs king, have a more specialized eval for checkmate)
- Add opposition eval for king endgame
- Also add king vs pawn vs king eval
- Add pruning to not search big sacrifices when a depth of 5+ (or some other arbitrary number) after the sac will not be searched
- Allow transposition table to have rotated boards

## Changelog -- Discontinued
- Started late (started 9/1/23, not all changes from 9/1/23 are listed)
- 9/1/23 - Added search extension for checks
- 9/1/23 - Included quiescence & extension search depth in final depth output
- 9/1/23 - Added node count
- 9/1/23 - Added increased pawn evaluation (passed pawn, isolated pawn, stacked pawn)
- 9/2/23 - Added larger depth extensions for endgames
- 9/2/23 - Added outpost eval (still need to test in addition to passed pawn)
- 9/2/23 - Added positional eval
- 9/2/23 - Added move lines (for full PV searched)
- 9/3/23 - Used move lines for more alpha-beta pruning
- 9/3/23 - Added null move pruning
- 9/4/23 - Removed move lines for alpha-beta pruning (as they gave no benefit and move lines are bugged)
- 9/4/23 - Removed null move pruning as it caused the bot to blunder pieces (possibly due to low search depths)
- 9/7/23 - Fix bug in black passed pawn evaluation bitmask