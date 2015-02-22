﻿using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Board
{
	using Ext;
	using Events;
	using Model;

	public class ScrabbleBoard : MonoBehaviour 
	{
		[SerializeField] private Tile m_tile;
		[SerializeField] private Tile[,] m_tileGrid;
		private List<Tile> m_tiles;
		private Model m_model;

		private void Awake ()
		{
			this.Assert<Tile>(m_tile, "m_tile must never be null!");

			m_model = Model.Instance;
			m_model.Scrabble = this;
			m_tileGrid = new Tile[BOARD.BOARD_ROWS, BOARD.BOARD_COLS];
			m_tiles = new List<Tile>();

			this.InitializeBoard();

			// initialize event
			ScrabbleEvent.Instance.OnTriggerEvent += this.OnEventListened;
		}

		private void Start ()
		{
			this.InitializeActiveTiles();
		}
		
		private void OnDestroy ()
		{
			ScrabbleEvent.Instance.OnTriggerEvent -= this.OnEventListened;
		}

		private void InitializeBoard ()
		{
			// show peg
			m_tile.gameObject.SetActive(true);

			for (int row = 0; row < BOARD.BOARD_ROWS; row++)
			{
				for (int col = 0; col < BOARD.BOARD_COLS; col++)
				{
					// generate tile
					Tile tile = this.CreateBoardTile(m_tile, ETileType.BK);
					tile.name = "Tile_" + row + "_" + col;
					tile.transform.parent = this.transform;

					// adjust position
					Vector3 position = m_tile.transform.position;
					position.x = (col * BOARD.TILE_OFFSET) - BOARD.TILE_SCREEN_OFFSET;
					position.y = (row * BOARD.TILE_OFFSET) - BOARD.TILE_SCREEN_OFFSET;
					tile.transform.position = position;

					// preload skin
					ETileType type = m_model.Board.MapFrom(row, col);
					tile.PreloadSkin(type, row, col);

					// set tile
					m_tileGrid[row, col] = tile;
					m_tiles.Add(tile);
				}
			}

			// hide peg
			m_tile.gameObject.SetActive(false);
		}

		private void InitializeActiveTiles ()
		{
			// deactivate all tiles
			foreach (Tile tile in m_tiles)
			{
				tile.Deactivate();
			}

			// activate initial tile
			m_tileGrid[m_model.Default.Row, m_model.Default.Col].Activate();
		}

		private void OnEventListened (EEvents p_type, IEventData p_data)
		{
			switch (p_type)
			{
				case EEvents.OnDrop:
				{
					DropEvent drop = (DropEvent)p_data;
					Vector3 pos = drop.Data<Vector3>(DropEvent.POSITION);
					Vector2 newPos = new Vector2(pos.x, pos.y);
					Letter letter = drop.Data<Letter>(DropEvent.LETTER);

					//this.Log(Tags.Log, "Scrabble::OnEventListened DropEvent OnPos:{0} Letter:{1}", pos, letter);
					
					Predicate<Tile> filter = (Tile tile) => { return tile.IsActive; };
					List<Tile> activeTiles = m_tiles.FindAll(filter);
					bool snapped = false;
					
					foreach (Tile tile in activeTiles)
					{
						//bool contains = tile.Rect.Contains(newPos);
						Rect rect = tile.Rect;
						bool contains = rect.center.x <= newPos.x &&
										rect.center.y <= newPos.y &&
										rect.size.x >= newPos.x &&
										rect.size.y >= newPos.y;

						if (contains)
						{
							this.Log(Tags.Log, "Snap!");
							snapped = true;
							
							// TODO: Trigger Snapping
							ScrabbleEvent.Instance.Trigger(EEvents.OnSnapped, new SnapEvent(tile, letter.Type));
							
							// Cleanup rack
							ScrabbleEvent.Instance.Trigger(EEvents.OnCleanUpRack, new SnapEvent(tile, letter.Type));

							// TODO: Trigger active neighbor tiles!
							this.EnableNeighbors();

							break;
						}
						else
						{
							this.Log(Tags.Log, "ScrabbleBoard::OnEventListener OnDrop Row:{0} Col:{1} IsActive:{2} Rect:{3} Pos:{4}", tile.TileModel.Row, tile.TileModel.Col, tile.TileModel.IsActive, tile.Rect, newPos);
						}
					}

					if (!snapped)
					{
						if (letter.Tile == null)
						{
							letter.Reset();
						}
						else
						{
							Model.Instance.Rack.AddLetter(letter);
						}

						this.EnableNeighbors();
					}
				}
				break;
			}
		}

		private void EnableNeighbors ()
		{
			// deactivate inactive tiles
			this.DisableNeighbors();

			Predicate<Tile> filter = (Tile tile) => { return tile.TileModel.Letter != null; };
			List<Tile> occupiedTiles = m_tiles.FindAll(filter);

			foreach (Tile tile in occupiedTiles)
			{
				this.EnableNeighbors(tile.TileModel.Row, tile.TileModel.Col, tile);
			}
		}

		private void DisableNeighbors ()
		{
			foreach (Tile tile in m_tiles)
			{
				if (tile.TileModel.Letter == null)
				{
					tile.Deactivate();
				}
			}

			// initial tile
			Tile initTile = m_tileGrid[m_model.Default.Row, m_model.Default.Col];
			if (initTile.TileModel.Letter == null)
			{
				initTile.Activate();
			}
		}

		/// <summary>
		/// Flood fill (1x1 neighbor)
		/// </summary>
		private void EnableNeighbors (int p_row, int p_col, Tile p_tile)
		{
			for (int row = -1; row < 2; row++)
			{
				for (int col = -1; col < 2; col++)
				{
					// blocked tiles
					if (row == 0 && col == 0) { continue; }
					if (row == 1 && col == 1) { continue; }
					if (row == -1 && col == -1) { continue; }
					if (row == 1 && col == -1) { continue; }
					if (row == -1 && col == 1) { continue; }

					int nRow = p_tile.TileModel.Row + row;
					int nCol = p_tile.TileModel.Col + col;

					if (nRow < 0 || nCol > BOARD.BOARD_COLS - 1) { continue; }

					Tile tile = m_tileGrid[nRow, nCol];

					// activate unoccupied tiles
					if (tile.TileModel.Letter == null)
					{
						tile.Activate();
					}
				}
			}
		}
	}
}