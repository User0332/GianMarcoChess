# GianMarco

A work-in-progress UCI-compatible chess engine written in C# utilizing a search-and-eval strategy. This project uses modified code from Sebastian Lague's `ChessChallenge.Chess` and `ChessChallenge.API` namespaces. Play the bot on lichess ([@GianMarcoChessBot](https://lichess.org/@/GianMarcoChessBot))!

## Todo
- Store move lines for further alpha-beta pruning (and use this with pv)
- Add more pruning techniques for increased depth