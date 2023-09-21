import subprocess
import threading
import re
from libai_whatajoke.evolve.simple_genetic import BinaryListGeneticAlgorithm

TOTAL_BITS = 10*3
LARGE_NUMBER = 100000000000000
LAST_RESULTS = [937, 347, 66]
LAST_RESULTS_BIN = [int(digit) for digit in ''.join(format(num, '#012b').removeprefix("0b") for num in LAST_RESULTS)]
START_POPULATION = ([LAST_RESULTS_BIN]*4)[:10]
TEST_POSITIONS = [
	"startpos",
	"startpos moves e2e4 b8c6 f1b5 g8f6 b5c6 d7c6 e4e5 f6d5 g1f3 c8f5 e1g1 d5b4 b1a3 d8d5 c2c4 d5d3 f1e1 e7e6 e1e3 d3d7 f3h4 f5d3 d1g4 h7h5 g4g5 d7d8 g5g3 g7g5 h4f3 f8c5 g3g5 d8g5 f3g5 c5e3 d2e3 d3g6 g5f3 b4d3 f3h4 d3e5 h4g6 e5g6 c1d2 e6e5 d2a5 a8c8 a5b4 e8d7 g1h1 d7e6 e3e4 g6f4 b4c5 b7b6 c5e3 f4d3 b2b3 d3b4",
	"startpos moves e2e4 b8c6 f1b5 g8f6 b5c6 d7c6 e4e5 f6d5 g1f3 c8f5 e1g1 d5b4",
	"startpos moves e2e4 b8c6 f1b5 g8f6 b5c6 d7c6 e4e5 f6d5 g1f3 c8f5 e1g1 d5b4 b1a3 d8d5 c2c4 d5d3 f1e1 e7e6 e1e3 d3d7 f3h4 f5d3 d1g4 h7h5 g4g5 d7d8 g5g3 g7g5 h4f3 f8c5 g3g5 d8g5 f3g5 c5e3 d2e3 d3g6 g5f3 b4d3 f3h4 d3e5 h4g6 e5g6 c1d2 e6e5 d2a5 a8c8 a5b4 e8d7 g1h1 d7e6 e3e4 g6f4 b4c5 b7b6 c5e3 f4d3 b2b3 d3b4 f2f4 e5f4 e3f4 b4d3 a1f1 d3f4 f1f4 e6e5 f4f7 c8f8 f7f3 e5e4 a3b1 e4d4 f3f8 h8f8 h1g1 d4d3 b1a3 f8e8 h2h3 e8e2 c4c5 b6b5 a3b5 c6b5 a2a3 e2a2 a3a4 b5a4 b3a4 a2a4",
	"startpos moves e2e4 b8c6 f1b5 g8f6 b5c6 d7c6 e4e5 f6d5 g1f3 c8f5 e1g1 d5b4 b1a3 d8d5 c2c4 d5d3 f1e1 e7e6 e1e3 d3d7 f3h4 f5d3 d1g4 h7h5 g4g5 d7d8 g5g3 g7g5 h4f3 f8c5 g3g5 d8g5 f3g5 c5e3 d2e3 d3g6 g5f3 b4d3 f3h4 d3e5 h4g6 e5g6 c1d2 e6e5 d2a5 a8c8 a5b4 e8d7 g1h1 d7e6 e3e4 g6f4 b4c5 b7b6 c5e3 f4d3 b2b3 d3b4 f2f4 e5f4 e3f4 b4d3 a1f1 d3f4 f1f4 e6e5 f4f7 c8f8 f7f3 e5e4 a3b1 e4d4 f3f8 h8f8 h1g1 d4d3 b1a3 f8e8 h2h3 e8e2 c4c5 b6b5 a3b5 c6b5 a2a3 e2a2 a3a4 b5a4 b3a4 a2a4 g1h2 h5h4 g2g4 a4c4 h2h1 c4c5 h1g1 d3e4",
	"startpos moves e2e3 b8c6 g1f3 g8f6",
	"startpos moves e2e4 b8c6 f1b5 g8f6 b5c6 d7c6 e4e5 f6d5 g1f3 c8f5 e1g1 d5b4 b1a3 d8d5",
	"startpos moves e2e4 b8c6 f1b5 g8f6 b5c6 d7c6 e4e5 f6d5 g1f3 c8f5 e1g1 d5b4 b1a3 d8d5 c2c4 d5d3 f1e1 e7e6 e1e3 d3d7 f3h4 f5d3 d1g4 h7h5 g4g5 d7d8 g5g3 g7g5 h4f3 f8c5 g3g5 d8g5 f3g5 c5e3 d2e3 d3g6",
]

def fitness(genome: list[int]):
	capture_bonus = str(int(''.join(str(x) for x in genome[:10]), base=2))
	promote_bonus = str(int(''.join(str(x) for x in genome[10:20]), base=2))
	castle_bonus = str(int(''.join(str(x) for x in genome[20:]), base=2))

	nodes_searched = 0

	num_complete = 0

	def test_engine(pos: str):
		nonlocal nodes_searched
		nonlocal num_complete

		proc = subprocess.Popen(["../GianMarco/bin/Release/net7.0/GianMarco.exe", capture_bonus, promote_bonus, castle_bonus], stdin=subprocess.PIPE, stdout=subprocess.PIPE)
		stdout = proc.communicate(input=f"position {pos}\ngo depth 5\nquit\n".encode())[0].decode()

		proc.kill()

		info_str = stdout.splitlines()[-2]

		nodes_searched+=int(
			re.match(".* nodes (\d+)", string=info_str).group(1)
		)

		num_complete+=1

	for pos in TEST_POSITIONS:
		threading.Thread(target=test_engine, args=(pos,)).start()

	while num_complete != len(TEST_POSITIONS): pass

	return LARGE_NUMBER-nodes_searched

alg = BinaryListGeneticAlgorithm(
	TOTAL_BITS,
	10,
	2,
	fitness,
	start_population=START_POPULATION
)

alg.train(generations=20)
genome = alg.get_best_model()

capture_bonus = int(''.join(str(x) for x in genome[:10]), base=2)
promote_bonus = int(''.join(str(x) for x in genome[10:20]), base=2)
castle_bonus = int(''.join(str(x) for x in genome[20:]), base=2)
				   
print(f"Best Results: {capture_bonus=} {promote_bonus=} {castle_bonus=}")
	