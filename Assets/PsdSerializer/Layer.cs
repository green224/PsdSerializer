using System;
using UnityEngine;

using System.Collections.Generic;


namespace PsdSerializer {
	
	sealed class Layer {
		//----------------------------------------------- publicフィールド ---------------------------------------------

		public string name="layer";						//!< レイヤー名
		public uint rect_t,rect_l,rect_b,rect_r;		//!< 範囲情報
		public ushort channelCnt=4;						//!< チャンネル数
		public BlendMode blendMode=BlendMode.Normal;	//!< ブレンドモード
		public byte opacity=0xFF;						//!< 不透明度
		public bool clipping=false;						//!< クリッピング中か否か
		public bool protectTrnsp=false;					//!< 透明度保護
		public bool visible=true;						//!< 表示しているか否か
		public Color[] colors;							//!< 内部のピクセル

		public uint width  => rect_r-rect_l;
		public uint height => rect_b-rect_t;

		/** テクスチャから生成する */
		public void generateFrom( TexInput tex ) {

			colors = tex.getColors();		// カラーを読み込み

			clipping = tex.isClipped;		// クリッピング状態
			visible = tex.isVisible;		// 表示非表示状態

			// 範囲情報を初期化
			rect_t=0;
			rect_l=0;
			rect_b=(uint)tex.height;
			rect_r=(uint)tex.width;
		}

		/** 正しい状態のデータかチェックする */
		public bool validate() {

			if ( string.IsNullOrEmpty(name) )
				{Debug.LogError("layers name is none"); return false;}

			if ( width < 1 || height < 1 )
				{Debug.LogError("layer["+name+"] w/h is Invalid:("+width+","+height+")"); return false;}

			if ( channelCnt < 1 || 4 < channelCnt )
				{Debug.LogError("layer["+name+"] channelCnt is Invalid:"+channelCnt); return false;}

			if ( colors == null || colors.Length != width*height )
				{Debug.LogError("layer["+name+"] colors length is Invalid:"+colors?.Length+" != "+(width*height)); return false;}

			return true;
		}


		//----------------------------------------- private / protected フィールド -------------------------------------


		//---------------------------------------------------------------------------------------------------------------
	}
}
