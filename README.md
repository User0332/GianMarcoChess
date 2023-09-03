# GianMarco

A work-in-progress UCI-compatible chess engine written in C# utilizing a search-and-eval strategy. This project uses modified code from Sebastian Lague's `ChessChallenge.Chess` and `ChessChallenge.API` namespaces. Play the bot on lichess ([@GianMarcoChessBot](https://lichess.org/@/GianMarcoChessBot))!

## Todo
- Add more pruning techniques for increased depth
- Look into why initial evals for passed pawns come out to ~ -300 cp
- Fix endgme checkmates (problem found w/ king & queen vs. king -- finds forced checkmate sequence but loses the sequence on the next few moves)
- Add pondering
- Use move lines in iterative deepening for alpha beta pruning (to counteract the slowdown caused by saving move lines)

## Changelog
- Started late (started 9/1/23, not all changes from 9/1/23 are listed)
- 9/1/23 - Added search extension for checks
- 9/1/23 - Included quiescence & extension search depth in final depth output
- 9/1/23 - Added node count
- 9/1/23 - Added increased pawn evaluation (passed pawn, isolated pawn, stacked pawn)
- 9/2/23 - Added larger depth extensions for endgames
- 9/2/23 - Added outpost eval (still need to test in addition to passed pawn)
- 9/2/23 - Added positional eval
- 9/2/23 - Added move lines (for full PV searched)