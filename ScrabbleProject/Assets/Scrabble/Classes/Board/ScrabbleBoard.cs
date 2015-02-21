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
		[SerializeField] private Tile[,] m_tiles;
		private Model m_model;

		private void Awake ()
		{
			this.Assert<Tile>(m_tile, "m_tile must never be null!");

			m_model = Model.Instance;
			m_tiles = new Tile[BOARD.BOARD_ROWS, BOARD.BOARD_COLS];

			this.InitializeBoard();
			this.InitializeActiveTiles();

			// initialize event
			ScrabbleEvent.Instance.OnTriggerEvent += this.OnEventListened;
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
					m_tiles[row, col] = tile;
				}
			}

			// hide peg
			m_tile.gameObject.SetActive(false);
		}

		private void InitializeActiveTiles ()
		{
			m_tiles[m_model.Default.Row, m_model.Default.Col].Activate();
		}

		private void OnEventListened (EEvents p_type, IEventData p_data)
		{
			switch (p_type)
			{
				case EEvents.OnDrop:
				{
					DropEvent drop = (DropEvent)p_data;
					Vector3 pos = drop.Data<Vector3>(DropEvent.POSITION);
					Letter letter = drop.Data<Letter>(DropEvent.LETTER);

					this.Log(Tags.Log, "Scrabble::OnEventListened DropEvent OnPos:{0} Letter:{1}", pos, letter);
				}
				break;
			}
		}
	}
}