using System;
using UnityEngine;


namespace PsdSerializer {
	
	/** レイヤーをテクスチャから生成するためのパラメータ */
	sealed class TexInput {

		/** 色を無理変えるためのセレクター。必要であれば指定する */
		public Func<Color,Color> colSelecter = null;

		public bool isClipped = false;		//!< クリッピング状態か否か

		public bool isVisible = true;		//!< 表示非表示状態

		// サイズ情報
		public int height => tex==null ? rt.height : tex.height;
		public int width  => tex==null ? rt.width  : tex.width;

		public TexInput(Texture2D tex) {this.tex = tex;}
		public TexInput(RenderTexture rt) {this.rt = rt;}

		/** カラーを配列で取得する */
		public Color[] getColors() {
			if (rt!=null) tex = generateTexFromRT(rt);

			var (w,h) = (tex.width,tex.height);
			var preColors = tex.GetPixels(0, 0, w, h, 0);

			// 縦方向が逆なので、反転する
			var ret = new Color[preColors.Length];
			if (colSelecter==null) {
				for (int x=0; x<w; ++x) for (int y=0; y<h; ++y)
					ret[x+y*w] = preColors[x+(h-y-1)*w];
			} else {
				for (int x=0; x<w; ++x) for (int y=0; y<h; ++y)
					ret[x+y*w] = colSelecter( preColors[x+(h-y-1)*w] );
			}

			if (rt!=null) GameObject.DestroyImmediate( tex );
			return ret;
		}



		Texture2D tex = null;
		RenderTexture rt = null;

		/** RenderTextureからTexture2Dを生成。使用しなくなったら破棄すること */
		Texture2D generateTexFromRT(RenderTexture rt) {
			var tex = new Texture2D( rt.width, rt.height, TextureFormat.RGBA32, false);
			var srcRT = RenderTexture.active;
			RenderTexture.active = rt;
			tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
			tex.Apply();
			RenderTexture.active = srcRT;

			return tex;
		}
	}
}
