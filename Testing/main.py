import chess.engine
import threading

OUTCOMES = {
	"Engine1 Victory": 0,
	"Engine2 Victory": 0,
	"Draw": 0
}

def run_test():

	engine1 = chess.engine.SimpleEngine.popen_uci("versions/LimitedNullPruning/GianMarco.exe")
	engine2 = chess.engine.SimpleEngine.popen_uci("versions/LessLimitedNullPruning/GianMarco.exe")

	board = chess.Board()

	while not board.is_game_over():
		result1 = engine1.play(board, limit=chess.engine.Limit(time=4))

		board.push(result1.move)

		if board.is_game_over(): break

		result2 = engine2.play(board, limit=chess.engine.Limit(time=4))

		board.push(result2.move)

	engine1.quit()
	engine2.quit()

	res = board.result()

	if res == "1/2-1/2": OUTCOMES["Draw"]+=1
	elif res == "1-0": OUTCOMES["Engine1 Victory"]+=1
	else: OUTCOMES["Engine2 Victory"]+=1

threads: list[threading.Thread] = []

for i in range(10):
	thread = threading.Thread(target=run_test)
	thread.start()

	threads.append(thread)

for thread in threads:
	while thread.is_alive(): pass

for name, amount in OUTCOMES.items():
	print(f"{name}: {amount}")