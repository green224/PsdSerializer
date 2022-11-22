using System;
using System.Collections.Generic;


namespace PsdSerializer {
	
	sealed class BinaryBuilder : IDisposable {
		//----------------------------------------------- publicフィールド ---------------------------------------------

		readonly public Document target;

		public byte[] bin => _buf.ToArray();

		public BinaryBuilder(Document target) {
			this.target = target;


			// § ヘッダ
			addAscii("8BPS");
			add2byte(1);
			add4byte(0);add2byte(0);
			add2byte( target.channelCnt );
			add4byte( target.height );
			add4byte( target.width );
			add2byte( target.colDepth );
			add2byte( target.colMode );

			// § カラーモードデータ
			add4byte(0);		// ここは空

			// § イメージリソース
			add4byte(0);		// ここも空

			// § レイヤー＆マスク
			var lnmSecBSM = new BufSzMan(this,4);		// セクションサイズは最後に入れる
			{
				// レイヤーインフォ
				var lInfoBSM = new BufSzMan(this,4);		// レイヤーインフォサイズは最後に入れる
				var cdSizeBSMMap = new Dictionary<Layer,BufSzMan[]>();		// チャンネルごとのデータ量は最後に入れる
				add2byte( (uint)target.layers.Count );
				foreach (var layer in target.layers) {
					add4byte( layer.rect_t );
					add4byte( layer.rect_l );
					add4byte( layer.rect_b );
					add4byte( layer.rect_r );
					add2byte( layer.channelCnt );

					// チャンネル情報
					var cdSizeBSM = new BufSzMan[layer.channelCnt];		// チャンネルごとのデータ量は最後に入れる
					for (int channel=0; channel<layer.channelCnt; ++channel ) {
						if (layer.channelCnt==4) {
							if (channel==0)	add2byte(0xffff);
							else add2byte((uint)(channel-1));
						} else {
							add2byte((uint)channel);
						}
						cdSizeBSM[channel] = new BufSzMan(this,4);
					}
					cdSizeBSMMap.Add(layer,cdSizeBSM);

					addAscii("8BIM");
					addAscii( layer.blendMode.key );
					add1byte( layer.opacity );
					add1byte( layer.clipping ? 1u : 0u );
					add1byte( (layer.protectTrnsp?1u:0u) | ((layer.visible?0u:1u)<<1) );
					add1byte(0);

					var extraDataBSM = new BufSzMan(this,4);		// エクストラデータサイズは最後に入れる
					add4byte(0);		// レイヤーマスクは無し

					// Layer Blending Ranges Data は 適当に決め打ちで入れておく
					add4byte( (uint)((layer.channelCnt+1)*8) );
					for (int i=0; i<layer.channelCnt+1; ++i)
						{ add4byte(0x00ffff); add4byte(0x00ffff); }

					addPascalStr( layer.name );

					// Transparency Shapes Layer情報を入れておく
					addAscii("8BIM");
					addAscii("tsly");
					add4byte(4);
					add1byte(1); add1byte(0);add1byte(0);add1byte(0);
					

					extraDataBSM.endRange();		// エクストラデータサイズを保存
				}

				// チャンネルごとのイメージデータ
				foreach (var layer in target.layers) {
					var layerH = (int)layer.height;
					var layerW = (int)layer.width;
					var cdSizeBSM = cdSizeBSMMap[layer];

					for (int channel=0; channel<layer.channelCnt; ++channel ) {
						add2byte(1);		// 圧縮形式はとりあえずRLE固定
						var nmlChannelIdx = layer.channelCnt==4 ? (channel+3)%4 : channel;

						cdSizeBSM[channel].beginRange();

						var sizeChunkPos = _buf.Count;
						for (int y=0; y<layerH; ++y) add2byte(0);
						
						for (int y=0; y<layerH; ++y) {
							_rle.proc(layer.colors, nmlChannelIdx, y*layerW, layerW);
							write2byte( sizeChunkPos+y*2, (uint)_rle.result.Count );
							addBytes( _rle.result );
						}

						// チャンネルデータサイズを保存
						cdSizeBSM[channel].endRange();
					}
				}

				// レイヤーインフォサイズを保存
				if (lInfoBSM.curSize % 2 == 1) add1byte(0);
				lInfoBSM.endRange();


				{// グローバルレイヤーマスクInfo
					// ここはとりあえず適当に決め打ち
					add4byte(0x0E);
					add2byte(0);
					add4byte(0); add4byte(0);
					add2byte(0);
					add1byte(0x80);
					add1byte(0);
				}

			}
//			lnmSecBSM.rangeSrc = lnmSecBSM.pos - 8;		//ここなぜか大き目の値になる
			lnmSecBSM.endRange();		// セクションサイズを保存


			{// § イメージデータ
				add2byte(1);		// 圧縮形式はとりあえずRLE固定

				var (w,h) = ((int)target.width, (int)target.height);

				var sizeChunkPos = _buf.Count;
				for (int channel=0; channel<target.channelCnt; ++channel ) {
					for (int y=0; y<h; ++y) add2byte(0);
				}
					
				for (int channel=0; channel<target.channelCnt; ++channel ) {
					for (int y=0; y<h; ++y) {
						_rle.proc(target.colors, channel, y*w, w);
						write2byte( sizeChunkPos+(channel*h+y)*2, (uint)_rle.result.Count );
						addBytes( _rle.result );
					}
				}
			}

		}

		public void Dispose() {
			_buf.Clear();
			_buf = null;
		}


		//----------------------------------------- private / protected フィールド -------------------------------------

		/** バッファサイズを後から入れる処理を簡略化するための機能 */
		sealed class BufSzMan {

			public int rangeSrc = -1;		//!< 開始位置。無理やり変更できるように外に出している

			public int pos => _pos;
			public int curSize =>
				rangeSrc==-1 ? (_parent._buf.Count-_pos-_byteWidth) : (_parent._buf.Count-rangeSrc);

			public BufSzMan(BinaryBuilder parent, int byteWidth) {
				_parent = parent;
				_pos = _parent._buf.Count;
				_byteWidth = byteWidth;

				if (_byteWidth==1)		_parent.add1byte(0);
				else if (_byteWidth==2)	_parent.add2byte(0);
				else if (_byteWidth==4)	_parent.add4byte(0);
				else throw new InvalidProgramException();
			}

			public void beginRange() {
				if (rangeSrc != -1) throw new InvalidProgramException();
				rangeSrc = _parent._buf.Count;
			}

			public void endRange() {
				if (_byteWidth==1)		_parent.write1byte( _pos, (uint)curSize );
				else if (_byteWidth==2)	_parent.write2byte( _pos, (uint)curSize );
				else if (_byteWidth==4)	_parent.write4byte( _pos, (uint)curSize );
				else throw new InvalidProgramException();
			}

			BinaryBuilder _parent;
			int _pos, _byteWidth;
		}


		List<byte> _buf = new List<byte>();
		RLE _rle = new RLE();

		// バッファにプリミティブなデータを入れる処理
		void addAscii(string str) { foreach (var i in str) _buf.Add( (byte)i ); }
		void add1byte(uint data) => _buf.Add( (byte)(data&0xFF) );
		void add2byte(uint data) { add1byte( (data&0xFF00) >> 8 ); add1byte( data ); }
		void add4byte(uint data) { add2byte( (data&0xFFFF0000) >> 16 ); add2byte(data); }
		void addPascalStr(string str, int pad=4) {
			var size = ( 1+str.Length + pad-1 ) / pad * pad -1;
			add1byte( (byte)str.Length );
			for (int i=0; i<size; ++i)
				add1byte( i<str.Length ? (uint)str[i] : 0 );
		}
		void addBytes(List<byte> data) => _buf.AddRange(data);

		// バッファの指定位置をプリミティブなデータ書き換える処理
		void write1byte(int pos, uint data) => _buf[pos] = (byte)(data&0xFF);
		void write2byte(int pos, uint data) {
			write1byte( pos,  (data&0xFF00) >> 8 );
			write1byte( pos+1, data );
		}
		void write4byte(int pos, uint data) {
			write2byte( pos,  (data&0xFFFF0000) >> 16 );
			write2byte( pos+2, data );
		}



		//---------------------------------------------------------------------------------------------------------------
	}
}
