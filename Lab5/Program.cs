using System;

namespace SimplexCargo
{
	public class Cargo
	{
		public int Id { get; set; }
		public string Name { get; set; }
		public double Weight { get; set; }    
		public double Volume { get; set; }    
		public double Price { get; set; }     
		public double Available { get; set; }
	}
	public class Compartment
	{
		public int Id { get; set; }
		public double VolumeLimit { get; set; } 
		public double WeightLimit { get; set; }   
	}
	public class SimplexResult
	{
		public double[] DecisionVariables { get; set; }
		public double OptimalProfit { get; set; }
		public double[,] FinalTable { get; set; }
	}
	public class SimplexSolver
	{
		public SimplexResult SolveWithIterations(Cargo[] cargos, Compartment[] compartments)
		{
			int numCargos = cargos.Length;
			int numCompartments = compartments.Length;
			int numDecision = numCargos * numCompartments;  // 5 * 3 = 15 переменных 
			// Ограничения: 5(по количеству) + 3(по весу) + 3(по объёму) = 11
			int numCargoConstraints = numCargos;
			int numWeightConstraints = numCompartments;
			int numVolumeConstraints = numCompartments;
			int totalConstraints = numCargoConstraints + numWeightConstraints + numVolumeConstraints;
			// Общее число переменных
			int totalVariables = numDecision + totalConstraints;
			int rows = totalConstraints + 1;
			int cols = totalVariables + 1;

			double[,] table = new double[rows, cols];

			int row = 0;
			// Ограничения по количеству груза:
			for (int i = 0; i < numCargos; i++)
			{
				for (int j = 0; j < numCompartments; j++)
				{
					int col = i * numCompartments + j;
					table[row, col] = 1.0;
				}
				table[row, numDecision + row] = 1.0;
				table[row, cols - 1] = cargos[i].Available;
				row++;
			}
			// Ограничения по весу для каждого отсека:
			for (int j = 0; j < numCompartments; j++)
			{
				for (int i = 0; i < numCargos; i++)
				{
					int col = i * numCompartments + j;
					table[row, col] = cargos[i].Weight;
				}
				table[row, numDecision + row] = 1.0;
				table[row, cols - 1] = compartments[j].WeightLimit;
				row++;
			}
			// Ограничения по объёму для каждого отсека:
			for (int j = 0; j < numCompartments; j++)
			{
				for (int i = 0; i < numCargos; i++)
				{
					int col = i * numCompartments + j;
					table[row, col] = cargos[i].Volume;
				}
				table[row, numDecision + row] = 1.0;
				table[row, cols - 1] = compartments[j].VolumeLimit;
				row++;
			}

			// Заполнение строки целевой функции 
			// При записи в симплекс-таблице коэффициенты записываются с минусом
			int objRow = rows - 1;
			for (int i = 0; i < numCargos; i++)
			{
				for (int j = 0; j < numCompartments; j++)
				{
					int col = i * numCompartments + j;
					table[objRow, col] = -cargos[i].Price;
				}
			}
			table[objRow, cols - 1] = 0;

			Console.WriteLine("Исходнаая симплекс-таблица:");
			PrintTable(table, rows, cols);

			// Основной цикл симплекс-метода
			int iteration = 0;
			while (true)
			{
				// Выбор входящей переменной
				// Поиск самого отрицательного коэффициента в строке цели
				int c = -1;
				double minValue = 0;
				for (int j = 0; j < cols - 1; j++)
				{
					if (table[objRow, j] < minValue)
					{
						minValue = table[objRow, j];
						c = j;
					}
				}
				if (c == -1)
					break;

				// Определяем разрешающую строку
				int r = -1;
				double minRatio = double.PositiveInfinity;
				for (int i = 0; i < rows - 1; i++)
				{
					if (table[i, c] > 0)
					{
						double ratio = table[i, cols - 1] / table[i, c];
						if (ratio < minRatio)
						{
							minRatio = ratio;
							r = i;
						}
					}
				}
				if (r == -1)
					throw new Exception("Задача неограничена.");

				double element = table[r, c];

				// Нормализация разрешающей строки
				for (int j = 0; j < cols; j++)
					table[r, j] /= element;

				// Обнуляем столбец входящей переменной во всех остальных строках
				for (int i = 0; i < rows; i++)
				{
					if (i != r)
					{
						double factor = table[i, c];
						for (int j = 0; j < cols; j++)
						{
							table[i, j] -= factor * table[r, j];
						}
					}
				}

				iteration++;
				Console.WriteLine($"\nИтерация {iteration}:");
				Console.WriteLine($"Входящая переменная: столбец {c}, Разрешающая строка: {r}");
				PrintTable(table, rows, cols);
			}

			// Извлечение оптимального решения
			double[] decision = new double[numDecision];
			for (int j = 0; j < numDecision; j++)
			{
				int basicRow = -1;
				bool isBasic = true;
				for (int i = 0; i < rows - 1; i++)
				{
					if (Math.Abs(table[i, j] - 1) < 1e-6)
					{
						if (basicRow == -1)
							basicRow = i;
						else { isBasic = false; break; }
					}
					else if (Math.Abs(table[i, j]) > 1e-6)
					{
						isBasic = false;
						break;
					}
				}
				decision[j] = (isBasic && basicRow != -1) ? table[basicRow, cols - 1] : 0;
			}
			double optimalProfit = table[objRow, cols - 1];

			return new SimplexResult { DecisionVariables = decision, OptimalProfit = optimalProfit, FinalTable = table };
		}

		// Метод для вывода симплекс-таблицы 
		private void PrintTable(double[,] table, int rows, int cols)
		{
			for (int i = 0; i < rows; i++)
			{
				for (int j = 0; j < cols; j++)
				{
					Console.Write($"{table[i, j],8:F2} ");
				}
				Console.WriteLine();
			}
		}
	}

	class Program
	{
		static void Main(string[] args)
		{
			// Исходные данные по грузам
			Cargo[] cargos = new Cargo[]
			{
				new Cargo { Id = 1, Name = "Трубы", Weight = 2.5, Volume = 7.6, Price = 34.5, Available = 600 },
				new Cargo { Id = 2, Name = "Бумага", Weight = 1.6, Volume = 1, Price = 21.5, Available = 1000 },
				new Cargo { Id = 3, Name = "Контейнеры", Weight = 5, Volume = 6.5, Price = 51, Available = 200 },
				new Cargo { Id = 4, Name = "Металлопрокат", Weight = 35, Volume = 6, Price = 275, Available = 200 },
				new Cargo { Id = 5, Name = "Пиломатериалы", Weight = 4, Volume = 6, Price = 110, Available = 350 }
			};

			// Данные по отсекам
			Compartment[] compartments = new Compartment[]
			{
				new Compartment { Id = 1, VolumeLimit = 1200, WeightLimit = 700 },
				new Compartment { Id = 2, VolumeLimit = 1000, WeightLimit = 800 },
				new Compartment { Id = 3, VolumeLimit = 1500, WeightLimit = 1300 }
			};

			Console.WriteLine("Решение для исходных данных:");
			SimplexSolver solver = new SimplexSolver();
			var resultInitial = solver.SolveWithIterations(cargos, compartments);
			PrintDistribution(resultInitial, cargos, compartments);
			Console.WriteLine($"Максимальная прибыль: {resultInitial.OptimalProfit:F2}");

			// Изменение количества грузов:
			// Пиломатериалы: 350 -> 400, Бумага: 1000 -> 900, Контейнеры: 200 -> 100.
			cargos[4].Available = 400; // Пиломатериалы
			cargos[1].Available = 900; // Бумага
			cargos[2].Available = 100; // Контейнеры

			Console.WriteLine("\nРешение после изменений:");
			var resultModified = solver.SolveWithIterations(cargos, compartments);
			PrintDistribution(resultModified, cargos, compartments);
			Console.WriteLine($"Максимальная прибыль: {resultModified.OptimalProfit:F2}");

			double profitDiff = resultModified.OptimalProfit - resultInitial.OptimalProfit;
			Console.WriteLine($"\nИзменение прибыли: {profitDiff:F2}");

			// Оценка выгодности перевозки грузов по итоговому решению 
			EvaluateProfitability(resultModified, cargos, compartments);

			Console.WriteLine("\nНажмите любую клавишу для завершения...");
			Console.ReadKey();
		}

		// Метод для группированного вывода распределения грузов по отсеку
		private static void PrintDistribution(SimplexResult result, Cargo[] cargos, Compartment[] compartments)
		{
			Console.WriteLine("\nОптимальное распределение грузов по отсеку:");
			int numCompartments = compartments.Length;
			for (int j = 0; j < numCompartments; j++)
			{
				Console.WriteLine($"Отсек {j + 1}:");
				bool hasCargo = false;
				for (int i = 0; i < cargos.Length; i++)
				{
					int index = i * numCompartments + j;
					double quantity = result.DecisionVariables[index];
					if (quantity > 1e-6)
					{
						Console.WriteLine($"    {cargos[i].Name}: {quantity:F2}");
						hasCargo = true;
					}
				}
				if (!hasCargo)
				{
					Console.WriteLine("    Нет грузов.");
				}
			}
		}

		// Метод для оценки выгодности перевозки грузов
		private static void EvaluateProfitability(SimplexResult result, Cargo[] cargos, Compartment[] compartments)
		{
			int numCompartments = compartments.Length;
			int totalConstraints = cargos.Length + 2 * numCompartments;
			int rows = totalConstraints + 1;

			Console.WriteLine("\nОценка выгодности перевозки грузов:");
			for (int i = 0; i < cargos.Length; i++)
			{
				bool isUsed = false;
				for (int j = 0; j < numCompartments; j++)
				{
					int index = i * numCompartments + j;
					if (result.DecisionVariables[index] > 1e-6)
					{
						isUsed = true;
						break;
					}
				}
				if (isUsed)
				{
					Console.WriteLine($"{cargos[i].Name} выгодно перевозить.");
				}
				else
				{
					double requiredIncrease = double.PositiveInfinity;
					for (int j = 0; j < numCompartments; j++)
					{
						int col = i * numCompartments + j;
						double reducedCost = result.FinalTable[rows - 1, col];
						if (reducedCost > 1e-6 && reducedCost < requiredIncrease)
						{
							requiredIncrease = reducedCost;
						}
					}
					if (requiredIncrease < double.PositiveInfinity)
					{
						Console.WriteLine($"{cargos[i].Name} невыгодно перевозить. " +
							$"Необходимо увеличить оплату за перевозку единицы на минимум {requiredIncrease:F2} ден.ед.");
					}
					else
					{
						Console.WriteLine($"{cargos[i].Name} не перевозится.");
					}
				}
			}
		}
	}
}
