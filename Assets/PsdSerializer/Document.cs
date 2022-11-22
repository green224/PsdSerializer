using System;
using UnityEngine;
using System.Collections.Generic;


namespace PsdSerializer {
	
	sealed class Document {
		//----------------------------------------------- publicフィールド ---------------------------------------------

		public ushort channelCnt=3;				//!< カラーチャンネル数
		public uint width=0, height=0;			//!< 画像サイズ
		public ushort colDepth=8;				//!< チャンネル当たりのビット数(1,8,16のいずれか)
		readonly public ushort colMode=3;		//!< カラーモード。これはとりあえず固定(RGB color)

		readonly public List<Layer> layers = new List<Layer>();

		public Color[] colors;			//!< レイヤ合成後の最終結果のカラー情報


		/** RTから生成する */
		public void generateFrom( (TexInput tex, string name)[] rtList ) {
			foreach (var i in rtList) {
				var layer = new Layer(){name = i.name};
				layer.generateFrom(i.tex);
				layers.Add(layer);

				width  = Math.Max(width,  layer.width);
				height = Math.Max(height, layer.height);
			}
 		}

		/** レイヤを合成して最終結果カラーをビルドする */
		public void buildColors() {
			
			if ( colors == null || colors.Length != width*height)
				colors = new Color[width*height];

			for (int i=0; i<width*height; ++i)
				colors[i] = new Color(0,0,0,0);

			foreach (var layer in layers) {
				int i = -1;
				var c = layer.colors;
				var bMode = layer.blendMode;

				for (uint y=layer.rect_l; y<layer.rect_r; ++y)
				for (uint x=layer.rect_t; x<layer.rect_b; ++x) {
					colors[x+y*width] = bMode.blend( c[++i], colors[x+y*width] );
				}
			}
		}

		/** 正しい状態のデータかチェックする */
		public bool validate() {
			if ( channelCnt < 1 || 3 < channelCnt )
				{Debug.LogError("channelCnt is Invalid:"+channelCnt); return false;}
			if ( colDepth!=1 && colDepth!=8 && colDepth!=16 )
				{Debug.LogError("colDepth is Invalid:"+colDepth); return false;}
			if ( colors == null || colors.Length != width*height )
				{Debug.LogError("colors length is Invalid:"+colors?.Length); return false;}

			if ( layers.Count == 0 )
				{Debug.LogError("layers length is Invalid:"+layers.Count); return false;}
			foreach (var i in layers) if (!i.validate()) return false;

			return true;
		}


		//----------------------------------------- private / protected フィールド -------------------------------------



		//---------------------------------------------------------------------------------------------------------------
	}
}
