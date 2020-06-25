using UnityEngine;
using System.Collections.Generic;

public class Liquid {

	// Valores de liquido da celula
	float MaxValue = 1.0f;
	float MinValue = 0.005f;

	// Liquido a mais que a cell acima, com compressao
	float MaxCompression = 0.25f;

	// Limites de flow por janela
	float MinFlow = 0.005f;
	float MaxFlow = 4f;

	// Velocidade do flow
	float FlowSpeed = 1f;

	// Modificacoes realizadas
	float[,] Diffs;

	public void Initialize(Cell[,] cells) {
		Diffs = new float[cells.GetLength (0), cells.GetLength (1)];
	}

	// Quantidade de Liquido com pressao
	float CalculateVerticalFlowValue(float remainingLiquid, Cell destination)
	{
		float sum = remainingLiquid + destination.Liquid;
		float value = 0;

		if (sum <= MaxValue) {
			value = MaxValue;
		} else if (sum < 2 * MaxValue + MaxCompression) {
			value = (MaxValue * MaxValue + sum * MaxCompression) / (MaxValue + MaxCompression);
		} else {
			value = (sum + MaxCompression) / 2f;
		}

		return value;
	}

	// Simular primeiro
	public void Simulate(ref Cell[,] cells) {

		float flow = 0;

		// Reseta array de modificacoes
		for (int x = 0; x < cells.GetLength (0); x++) {
			for (int y = 0; y < cells.GetLength (1); y++) {
				Diffs [x, y] = 0;
			}
		}

		// MAIN
		for (int x = 0; x < cells.GetLength(0); x++) {
			for (int y = 0; y < cells.GetLength(1); y++) {

				// Referencia celula e reseta flow
				Cell cell = cells [x, y];
				cell.ResetFlowDirections ();

				// Valida celula
				if (cell.Type == CellType.Solid) {
					cell.Liquid = 0;
					continue;
				}
				if (cell.Liquid == 0)
					continue;
				if (cell.Settled) 
					continue;
				if (cell.Liquid < MinValue) {
					cell.Liquid = 0;
					continue;
				}

				// Quanto liquido a celula tem desde o inicio
				float startValue = cell.Liquid;
				float remainingValue = cell.Liquid;
				flow = 0;

				// Flow para baixo
				if (cell.Bottom != null && cell.Bottom.Type == CellType.Blank) {

					// Determina velocidade do flow
					flow = CalculateVerticalFlowValue(cell.Liquid, cell.Bottom) - cell.Bottom.Liquid;
					if (cell.Bottom.Liquid > 0 && flow > MinFlow)
						flow *= FlowSpeed; 

					// Restringe o flow
					flow = Mathf.Max (flow, 0);
					if (flow > Mathf.Min(MaxFlow, cell.Liquid)) 
						flow = Mathf.Min(MaxFlow, cell.Liquid);

					// Update dos temps
					if (flow != 0) {
						remainingValue -= flow;
						Diffs [x, y] -= flow;
						Diffs [x, y + 1] += flow;
						cell.FlowDirections[(int)FlowDirection.Bottom] = true;
						cell.Bottom.Settled = false;
					} 
				}

				// Checa se ainda tem liquido na celula
				if (remainingValue < MinValue) {
					Diffs [x, y] -= remainingValue;
					continue;
				}

				// Flow pra esquerda
				if (cell.Left != null && cell.Left.Type == CellType.Blank) {

					// Calcula velocidade do flow
					flow = (remainingValue - cell.Left.Liquid) / 4f;
					if (flow > MinFlow)
						flow *= FlowSpeed;

					// Restringe flow
					flow = Mathf.Max (flow, 0);
					if (flow > Mathf.Min(MaxFlow, remainingValue)) 
						flow = Mathf.Min(MaxFlow, remainingValue);

					// Update dos temps
					if (flow != 0) {
						remainingValue -= flow;
						Diffs [x, y] -= flow;
						Diffs [x - 1, y] += flow;
						cell.FlowDirections[(int)FlowDirection.Left] = true;
						cell.Left.Settled = false;
					} 
				}

				// Checa se ainda tem liquido na celula
				if (remainingValue < MinValue) {
					Diffs [x, y] -= remainingValue;
					continue;
				}
				
				// Flow pra direita
				if (cell.Right != null && cell.Right.Type == CellType.Blank) {

                    // Calcula velocidade do flow
                    flow = (remainingValue - cell.Right.Liquid) / 3f;										
					if (flow > MinFlow)
						flow *= FlowSpeed;

                    // Restringe flow
                    flow = Mathf.Max (flow, 0);
					if (flow > Mathf.Min(MaxFlow, remainingValue)) 
						flow = Mathf.Min(MaxFlow, remainingValue);

                    // Update dos temps
                    if (flow != 0) {
						remainingValue -= flow;
						Diffs [x, y] -= flow;
						Diffs [x + 1, y] += flow;
						cell.FlowDirections[(int)FlowDirection.Right] = true;
						cell.Right.Settled = false;
					} 
				}

                // Checa se ainda tem liquido na celula
                if (remainingValue < MinValue) {
					Diffs [x, y] -= remainingValue;
					continue;
				}
				
				// Flow pra cima
				if (cell.Top != null && cell.Top.Type == CellType.Blank) {

                    // Calcula velocidade do flow
                    flow = remainingValue - CalculateVerticalFlowValue (remainingValue, cell.Top); 
					if (flow > MinFlow)
						flow *= FlowSpeed;

                    // Restringe flow
                    flow = Mathf.Max (flow, 0);
					if (flow > Mathf.Min(MaxFlow, remainingValue)) 
						flow = Mathf.Min(MaxFlow, remainingValue);

                    // Update dos temps
                    if (flow != 0) {
						remainingValue -= flow;
						Diffs [x, y] -= flow;
						Diffs [x, y - 1] += flow;
						cell.FlowDirections[(int)FlowDirection.Top] = true;
						cell.Top.Settled = false;
					} 
				}

                // Checa se ainda tem liquido na celula
                if (remainingValue < MinValue) {
					Diffs [x, y] -= remainingValue;
					continue;
				}

				// Checa se acabou o flow
				if (startValue == remainingValue) {
					cell.SettleCount++;
					if (cell.SettleCount >= 10) {
						cell.ResetFlowDirections ();
						cell.Settled = true;
					}
				} else {
					cell.UnsettleNeighbors ();
				}
			}
		}
			
		// Update dos valores da celula
		for (int x = 0; x < cells.GetLength (0); x++) {
			for (int y = 0; y < cells.GetLength (1); y++) {
				cells [x, y].Liquid += Diffs [x, y];
				if (cells [x, y].Liquid < MinValue) {
					cells [x, y].Liquid = 0;
					cells [x, y].Settled = false;	//celula default para inicio
				}				
			}
		}			
	}

}
