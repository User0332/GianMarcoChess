using System.Numerics;
using ChessChallenge.API;
using GianMarco.Evaluation;

namespace GianMarco.Search.Utils;

public static class StaticExchangeEvaluation
{
	public static bool IsLosingCapture(Board board, Move move)
	{
		return EvaluateCapture(board, move) < 0;
	}

	public static int EvaluateMove(Board board, Move move)
	{
		if (move.IsCapture)
		{
			return EvaluateCapture(board, move);
		}
		else if (!move.IsPromotion && !move.IsCastles && !move.IsEnPassant)
		{
			return EvaluateQuiet(board, move);
		}

		return 0;
	}

	public static int EvaluateQuiet(Board board, Move move)
	{
		int gain = 0;

		ulong whitePieces = board.WhitePiecesBitboard;
		ulong blackPieces = board.BlackPiecesBitboard;

		ulong whitePawns = board.GetPieceBitboard(PieceType.Pawn, true);
		ulong blackPawns = board.GetPieceBitboard(PieceType.Pawn, false);
		ulong whiteKnights = board.GetPieceBitboard(PieceType.Knight, true);
		ulong blackKnights = board.GetPieceBitboard(PieceType.Knight, false);
		ulong whiteBishops = board.GetPieceBitboard(PieceType.Bishop, true);
		ulong blackBishops = board.GetPieceBitboard(PieceType.Bishop, false);
		ulong whiteRooks = board.GetPieceBitboard(PieceType.Rook, true);
		ulong blackRooks = board.GetPieceBitboard(PieceType.Rook, false);
		ulong whiteQueens = board.GetPieceBitboard(PieceType.Queen, true);
		ulong blackQueens = board.GetPieceBitboard(PieceType.Queen, false);
		ulong whiteKings = board.GetPieceBitboard(PieceType.King, true);
		ulong blackKings = board.GetPieceBitboard(PieceType.King, false);

		ulong[,] pieceBitboards = new ulong[2, 6]
		{
			{ whitePawns, whiteKnights, whiteBishops, whiteRooks, whiteQueens, whiteKings },
			{ blackPawns, blackKnights, blackBishops, blackRooks, blackQueens, blackKings }
		};

		if (board.IsWhiteToMove) // white made the move
		{
			// remove from start
			whitePieces &= ~(1ul << move.StartSquare.Index);
			pieceBitboards[0, (int)move.MovePieceType - 1] &= ~(1ul << move.StartSquare.Index);

			// add moving piece on target
			whitePieces |= 1ul << move.TargetSquare.Index;
			pieceBitboards[0, (int)move.MovePieceType - 1] |= 1ul << move.TargetSquare.Index;
		}
		else // black made the move
		{
			blackPieces &= ~(1ul << move.StartSquare.Index);
			pieceBitboards[1, (int)move.MovePieceType - 1] &= ~(1ul << move.StartSquare.Index);

			blackPieces |= 1ul << move.TargetSquare.Index;
			pieceBitboards[1, (int)move.MovePieceType - 1] |= 1ul << move.TargetSquare.Index;
		}


		ulong allPieces = whitePieces | blackPieces;

		ulong attackers = GetAttackers(pieceBitboards, move.TargetSquare, allPieces);
		bool whiteToMove = !board.IsWhiteToMove; // after the move, it would be the opponent's turn, so we check for their attackers

		while ((attackers & (whiteToMove ? whitePieces : blackPieces)) != 0)
		{
			var (sq, type) = FindLeastValuableAttacker(pieceBitboards, attackers, whiteToMove);
			int pieceValue = Material.GetPieceValue(type);

			gain = pieceValue - gain; // don't cover promotions for now

			if (gain < 0)
				return gain;

			// remove the attacking piece from the board
			if (whiteToMove)
			{
				whitePieces &= ~(1ul << sq);
				pieceBitboards[0, (int)type - 1] &= ~(1ul << sq);
			}
			else
			{
				blackPieces &= ~(1ul << sq);
				pieceBitboards[1, (int)type - 1] &= ~(1ul << sq);
			}


			allPieces = whitePieces | blackPieces;

			attackers = GetAttackers(pieceBitboards, move.TargetSquare, allPieces);

			whiteToMove = !whiteToMove;
		}

		return gain;
	}

	public static int EvaluateCapture(Board board, Move move)
	{
		int gain = Material.GetPieceValue(move.CapturePieceType);

		ulong whitePieces = board.WhitePiecesBitboard;
		ulong blackPieces = board.BlackPiecesBitboard;

		ulong whitePawns = board.GetPieceBitboard(PieceType.Pawn, true);
		ulong blackPawns = board.GetPieceBitboard(PieceType.Pawn, false);
		ulong whiteKnights = board.GetPieceBitboard(PieceType.Knight, true);
		ulong blackKnights = board.GetPieceBitboard(PieceType.Knight, false);
		ulong whiteBishops = board.GetPieceBitboard(PieceType.Bishop, true);
		ulong blackBishops = board.GetPieceBitboard(PieceType.Bishop, false);
		ulong whiteRooks = board.GetPieceBitboard(PieceType.Rook, true);
		ulong blackRooks = board.GetPieceBitboard(PieceType.Rook, false);
		ulong whiteQueens = board.GetPieceBitboard(PieceType.Queen, true);
		ulong blackQueens = board.GetPieceBitboard(PieceType.Queen, false);
		ulong whiteKings = board.GetPieceBitboard(PieceType.King, true);
		ulong blackKings = board.GetPieceBitboard(PieceType.King, false);

		ulong[,] pieceBitboards = new ulong[2, 6]
		{
			{ whitePawns, whiteKnights, whiteBishops, whiteRooks, whiteQueens, whiteKings },
			{ blackPawns, blackKnights, blackBishops, blackRooks, blackQueens, blackKings }
		};

		if (board.IsWhiteToMove) // white made the capture
		{
			// remove from start
			whitePieces &= ~(1ul << move.StartSquare.Index);
			pieceBitboards[0, (int)move.MovePieceType - 1] &= ~(1ul << move.StartSquare.Index);

			// remove captured piece
			blackPieces &= ~(1ul << move.TargetSquare.Index);
			pieceBitboards[1, (int)move.CapturePieceType - 1] &= ~(1ul << move.TargetSquare.Index);

			// add moving piece on target
			whitePieces |= 1ul << move.TargetSquare.Index;
			pieceBitboards[0, (int)move.MovePieceType - 1] |= 1ul << move.TargetSquare.Index;
		}
		else // black made the capture
		{
			blackPieces &= ~(1ul << move.StartSquare.Index);
			pieceBitboards[1, (int)move.MovePieceType - 1] &= ~(1ul << move.StartSquare.Index);

			whitePieces &= ~(1ul << move.TargetSquare.Index);
			pieceBitboards[0, (int)move.CapturePieceType - 1] &= ~(1ul << move.TargetSquare.Index);

			blackPieces |= 1ul << move.TargetSquare.Index;
			pieceBitboards[1, (int)move.MovePieceType - 1] |= 1ul << move.TargetSquare.Index;
		}


		ulong allPieces = whitePieces | blackPieces;

		ulong attackers = GetAttackers(pieceBitboards, move.TargetSquare, allPieces);
		bool whiteToMove = !board.IsWhiteToMove; // after the capture, it would be the opponent's turn, so we check for their attackers

		while ((attackers & (whiteToMove ? whitePieces : blackPieces)) != 0)
		{
			var (sq, type) = FindLeastValuableAttacker(pieceBitboards, attackers, whiteToMove);
			int pieceValue = Material.GetPieceValue(type);

			gain = pieceValue - gain; // don't cover promotions for now

			if (gain < 0)
				return gain;

			// remove the attacking piece from the board
			if (whiteToMove)
			{
				whitePieces &= ~(1ul << sq);
				pieceBitboards[0, (int)type - 1] &= ~(1ul << sq);
			}
			else
			{
				blackPieces &= ~(1ul << sq);
				pieceBitboards[1, (int)type - 1] &= ~(1ul << sq);
			}


			allPieces = whitePieces | blackPieces;

			attackers = GetAttackers(pieceBitboards, move.TargetSquare, allPieces);

			whiteToMove = !whiteToMove;
		}

		return gain;
	}

	static (int SquareIndex, PieceType PieceType) FindLeastValuableAttacker(ulong[,] pieceBitboards, ulong attackers, bool isWhite)
	{
		int colorIndex = isWhite ? 0 : 1;

		for (int pieceType = 1; pieceType <= 6; pieceType++)
		{
			ulong piecesOfType = pieceBitboards[colorIndex, pieceType-1] & attackers;

			if (piecesOfType != 0)
			{
				return (BitOperations.TrailingZeroCount(piecesOfType), (PieceType) pieceType);
			}
		}

		return (-1, (PieceType) (-1)); // no attacker found
	}

	static ulong GetAttackers(ulong[,] pieceBitboards, Square targetSquare, ulong allPieces)
	{
		ulong attackers = 0;

		var whitePawns = pieceBitboards[0, 0];
		var blackPawns = pieceBitboards[1, 0];
		var knights = pieceBitboards[0, 1] | pieceBitboards[1, 1];
		var bishops = pieceBitboards[0, 2] | pieceBitboards[1, 2];
		var rooks = pieceBitboards[0, 3] | pieceBitboards[1, 3];
		var queens = pieceBitboards[0, 4] | pieceBitboards[1, 4];
		var kings = pieceBitboards[0, 5] | pieceBitboards[1, 5];

		attackers |= BitboardHelper.GetPawnAttacks(targetSquare, false) & whitePawns; // reversed case because pawns only attack forwards & BitBoardHelper.GetPawnAttacks returns the squares attacked by the pawn of the given color on that square
		attackers |= BitboardHelper.GetPawnAttacks(targetSquare, true) & blackPawns;
		attackers |= BitboardHelper.GetKnightAttacks(targetSquare) & knights;
		attackers |= BitboardHelper.GetKingAttacks(targetSquare) & kings;
		attackers |= BitboardHelper.GetBishopAttacks(targetSquare, allPieces) & (bishops | queens);
		attackers |= BitboardHelper.GetRookAttacks(targetSquare, allPieces) & (rooks | queens);

		return attackers;
	}
}