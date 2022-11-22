using System;
using UnityEngine;
using System.Collections.Generic;


namespace PsdSerializer {
	
	/**
	 * RLE圧縮したデータを取得する処理
	 * 参考：https://github.com/psd-tools/packbits/blob/master/src/packbits.py
	 */
	sealed class RLE {
		//----------------------------------------------- publicフィールド ---------------------------------------------

		/** コンバート結果 */
		readonly public List<byte> result = new List<byte>();

		/** 指定のカラー配列の、指定オフセットから指定サイズ分をRLE圧縮する */
		public void proc(Color[] tgt, int channel, int offset, int size) {
			_tgtCnvBuf.Clear();
			for (int i=0; i<size; ++i) {
				var c = tgt[ offset+i ];

				float aF;
				if (channel==0)			aF = c.r;
				else if (channel==1)	aF = c.g;
				else if (channel==2)	aF = c.b;
				else					aF = c.a;

				_tgtCnvBuf.Add( (byte)(aF*255 + 0.5f) );
			}

			proc(_tgtCnvBuf, 0, size);
		}

		/** 指定のバイト配列の、指定オフセットから指定サイズ分をRLE圧縮する */
		public void proc(List<byte> tgt, int offset, int size) {
			result.Clear();
			_buf.Clear();
			_repeatCnt = 0;

			var isRaw = true;

			for (int i=0; i<size-1; ++i) {
				var a = tgt[offset+i];

				if (a == tgt[offset+i+1]) {
					if (isRaw) {
						finishRaw();
						isRaw = false;
						_repeatCnt = 1;
						_buf.Add(a);
					} else {
						if (_repeatCnt == MaxLength) finishRLE();
						++_repeatCnt;
					}
				} else {
					if (isRaw) {
						if (_repeatCnt == MaxLength) finishRaw();
						_buf.Add(a);
					} else {
						++_repeatCnt;
						finishRLE();
						_buf.Clear();
						isRaw = true;
					}
				}
			}

			if (isRaw) {
				_buf.Add(tgt[offset+size-1]);
				finishRaw();
			} else {
				++_repeatCnt;
				finishRLE();
			}
		}

		//----------------------------------------- private / protected フィールド -------------------------------------

		const int MaxLength = 127;		//!< 不連続データ・連続データが続くことのできる最大長
		readonly List<byte> _tgtCnvBuf = new List<byte>();		//!< 型変換用のバッファ
		readonly List<byte> _buf = new List<byte>();			//!< 変換処理中の途中計算用バッファ
		int _repeatCnt;											//!< 変換処理中の連続データカウンタ

		void finishRaw() {
			if (_buf.Count==0) return;
			result.Add((byte)(_buf.Count - 1));
			result.AddRange(_buf);
			_buf.Clear();
		}
		
		void finishRLE() {
			result.Add( (byte)(0x100 - (_repeatCnt-1)) );
			result.Add( _buf[0] );
			_repeatCnt = 0;
		}


		//---------------------------------------------------------------------------------------------------------------
	}
}
