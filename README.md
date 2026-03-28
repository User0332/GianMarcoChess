# GianMarco

A work-in-progress UCI-compatible chess engine written in C# utilizing a search-and-eval strategy. This project uses modified code from Sebastian Lague's `ChessChallenge.Chess` and `ChessChallenge.API` namespaces. Play the bot on lichess ([@GianMarcoChessBot](https://lichess.org/@/GianMarcoChessBot))!

## Key Heuristics & Search Features
- Iterative Deepening
- Principal Variation Search
- Late Move Reduction
- Null Move Pruning
- Simple Transposition Table
- Move Ordering
	- SEE
	- Killer Moves
	- History Heuristic
- SEE Pruning (Quiescence Search)
- Delta Pruning (Quiescence Search)

## Key Evaluation Heuristics
- Material Evaluation
- Positional Evaluation
- King Safety Evaluation
- Pawn Evals
	- Stacked Pawns
	- Passed Pawns
	- Isolated Pawns