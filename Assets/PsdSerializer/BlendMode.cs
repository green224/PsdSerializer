using System;
using UnityEngine;


namespace PsdSerializer {

	/** ブレンドモード */
	public abstract class BlendMode {
		//----------------------------------------------- publicフィールド ---------------------------------------------

		// ブレンドモード一覧
		static readonly public BlendMode
			PassThrough		= new Impl_PassThrough(),
			Normal			= new Impl_Normal(),
			Dissolve		= new Impl_Dissolve(),
			Darken			= new Impl_Darken(),
			Multiply		= new Impl_Multiply(),
			ColorBurn		= new Impl_ColorBurn(),
			LinearBurn		= new Impl_LinearBurn(),
			DarkerColor		= new Impl_DarkerColor(),
			Lighten			= new Impl_Lighten(),
			Screen			= new Impl_Screen(),
			ColorDodge		= new Impl_ColorDodge(),
			LinearDodge		= new Impl_LinearDodge(),
			LighterColor	= new Impl_LighterColor(),
			Overlay			= new Impl_Overlay(),
			SoftLight		= new Impl_SoftLight(),
			HardLight		= new Impl_HardLight(),
			VividLight		= new Impl_VividLight(),
			LinearLight		= new Impl_LinearLight(),
			PinLight		= new Impl_PinLight(),
			HardMix			= new Impl_HardMix(),
			Difference		= new Impl_Difference(),
			Exclusion		= new Impl_Exclusion(),
			Subtract		= new Impl_Subtract(),
			Divide			= new Impl_Divide(),
			Hue				= new Impl_Hue(),
			Saturation		= new Impl_Saturation(),
			Color			= new Impl_Color(),
			Luminosity		= new Impl_Luminosity();

		public readonly string key;		//!< フォーマットに書き出す時のキー名

		public Color blend(Color src, Color dst) =>	//!< ブレンドを実行する
			blend((C)src, (C)dst);


		//----------------------------------------- private / protected フィールド -------------------------------------

		abstract public C blend(C src, C dst);	//!< ブレンドを実行する


		protected BlendMode(string key) => this.key = key;


		// 参考：https://photoblogstop.com/photoshop/photoshop-blend-modes-explained
		// 参考：https://ofo.jp/osakana/cgtips/blendmode.phtml
		protected float simpleBlendA(float src, float dst) => dst + (1f-dst)*src;
		protected float simpleBlend(float combined1n1, float srcC, float srcA, float dstC, float dstA) =>
			(combined1n1-dstC) * (srcA*dstA + (1f-dstA)) + dstC;
		protected C simpleBlend(C combined1n1, C src, C dst) =>
			new C(
				simpleBlend(combined1n1.r, src.r, src.a, dst.r, dst.a),
				simpleBlend(combined1n1.g, src.g, src.a, dst.g, dst.a),
				simpleBlend(combined1n1.b, src.b, src.a, dst.b, dst.a),
				simpleBlendA(src.a, dst.a)
			);

		sealed class Impl_PassThrough : BlendMode {
			public Impl_PassThrough() : base("pass") {}
			override public C blend(C src, C dst) =>
				throw new InvalidOperationException();
		}
		sealed class Impl_Normal : BlendMode {
			public Impl_Normal() : base("norm") {}
			override public C blend(C src, C dst) =>
				simpleBlend( src, src, dst );
		}
		sealed class Impl_Dissolve : BlendMode {
			public Impl_Dissolve() : base("diss") {}
			override public C blend(C src, C dst) =>
				throw new InvalidProgramException();		// ディザ合成は未実装
		}
		sealed class Impl_Darken : BlendMode {
			public Impl_Darken() : base("dark") {}
			override public C blend(C src, C dst) =>
				simpleBlend( C.min(src,dst), src, dst );
		}
		sealed class Impl_Multiply : BlendMode {
			public Impl_Multiply() : base("mul ") {}
			override public C blend(C src, C dst) =>
				simpleBlend( src*dst, src, dst );
		}
		sealed class Impl_ColorBurn : BlendMode {
			public Impl_ColorBurn() : base("idiv") {}
			override public C blend(C src, C dst) =>
				simpleBlend( ( dst.inv / src ).inv, src, dst );
		}
		sealed class Impl_LinearBurn : BlendMode {
			public Impl_LinearBurn() : base("lbrn") {}
			override public C blend(C src, C dst) =>
				simpleBlend( src+dst-1, src, dst );
		}
		sealed class Impl_DarkerColor : BlendMode {
			public Impl_DarkerColor() : base("dkCl") {}
			override public C blend(C src, C dst) =>
				simpleBlend( src.r+src.g+src.b < dst.r+dst.g+dst.b ? src : dst, src, dst );
		}
		sealed class Impl_Lighten : BlendMode {
			public Impl_Lighten() : base("lite") {}
			override public C blend(C src, C dst) =>
				simpleBlend( C.max(src,dst), src, dst );
		}
		sealed class Impl_Screen : BlendMode {
			public Impl_Screen() : base("scrn") {}
			override public C blend(C src, C dst) =>
				simpleBlend( (src.inv * dst.inv).inv, src, dst );
		}
		sealed class Impl_ColorDodge : BlendMode {
			public Impl_ColorDodge() : base("div ") {}
			override public C blend(C src, C dst) =>
				simpleBlend( dst / src.inv, src, dst );
		}
		sealed class Impl_LinearDodge : BlendMode {
			public Impl_LinearDodge() : base("lddg") {}
			override public C blend(C src, C dst) =>
				simpleBlend( src+dst, src, dst );
		}
		sealed class Impl_LighterColor: BlendMode {
			public Impl_LighterColor() : base("lgCl") {}
			override public C blend(C src, C dst) =>
				simpleBlend( src.r+src.g+src.b < dst.r+dst.g+dst.b ? dst : src, src, dst );
		}
		sealed class Impl_Overlay : BlendMode {
			public Impl_Overlay() : base("over") {}
			override public C blend(C src, C dst) =>
				simpleBlend(
					(src < 0.5f).branch( 2*src*dst, (src.inv * dst.inv * 2).inv ),
					src, dst
				);
		}
		sealed class Impl_SoftLight : BlendMode {
			public Impl_SoftLight() : base("sLit") {}
			override public C blend(C src, C dst) =>
				// 参考：https://en.wikipedia.org/wiki/Blend_modes#Overlay
				simpleBlend(
					(dst < 0.5f).branch(
						// これ、出典によって書いてあることが違う。とりあえずWikipediaのものを実装してみた
						2*src*dst + src*src*(2*dst).inv,
						2*src*dst.inv + src.sqrt*(2*dst-1)
					), src, dst
				);
		}
		sealed class Impl_HardLight : BlendMode {
			public Impl_HardLight() : base("hLit") {}
			override public C blend(C src, C dst) =>
				// 参考：https://ofo.jp/osakana/cgtips/blendmode.phtml
				simpleBlend(
					(dst < 0.5f).branch( src*dst*2, (src.inv*dst.inv*2).inv ),
					src, dst
				);
		}
		sealed class Impl_VividLight : BlendMode {
			public Impl_VividLight() : base("vLit") {}
			override public C blend(C src, C dst) =>
				// 参考：https://steamcommunity.com/app/365670/discussions/0/1489992713698183538/
				//       http://www.deepskyCs.com/archive/2010/04/21/formulas-for-Photoshop-blending-modes.html
				simpleBlend(
					(src < 0.5f).branch(
						dst / (src*2).inv,
						( dst.inv / (2*src-1) ).inv
					), src, dst
				);
		}
		sealed class Impl_LinearLight : BlendMode {
			public Impl_LinearLight() : base("lLit") {}
			override public C blend(C src, C dst) =>
				// ここに書いてある内容は間違っていると思っていた、
				// なぜなら式変形すると明るい部分と暗い部分で同じ式になるからである。
				// 参考：http://www.deepskyCs.com/archive/2010/04/21/formulas-for-Photoshop-blending-modes.html
				//
				// ここにも明部と暗部で異なる挙動と書いてある。
				// 参考：https://photoblogstop.com/photoshop/photoshop-blend-modes-explained
				//
				// ところがこの情報を元に式を組み立てても結局同じ式になるので、
				// もともとこのBlendModeは1つの線形な写像であるようだ。
				simpleBlend( dst+2*src-1, src, dst );
		}
		sealed class Impl_PinLight : BlendMode {
			public Impl_PinLight() : base("pLit") {}
			override public C blend(C src, C dst) =>
				// 参考：http://www.deepskyCs.com/archive/2010/04/21/formulas-for-Photoshop-blending-modes.html
				simpleBlend(
					(src < 0.5f).branch(
						C.min( dst, 2*src ),
						C.max( dst, 2*src-1 )
					), src, dst
				);
		}
		sealed class Impl_HardMix : BlendMode {
			public Impl_HardMix() : base("hMix") {}
			override public C blend(C src, C dst) =>
				// ここを参考に勘で実装した。実際に合っているかどうかは未確認
				// 参考：https://photoblogstop.com/photoshop/photoshop-blend-modes-explained
				// 参考：https://docs.krita.org/en/reference_manual/blending_modes/mix.html#hard-mix-photoshop
				simpleBlend(
					// LinearLightの計算結果がdstより小さければ0,大きければ1
					dst < (dst+2*src-1),
					src, dst
				);
		}
		sealed class Impl_Difference : BlendMode {
			public Impl_Difference() : base("diff") {}
			override public C blend(C src, C dst) =>
				simpleBlend( (src-dst).abs, src, dst );
		}
		sealed class Impl_Exclusion : BlendMode {
			public Impl_Exclusion() : base("smud") {}
			override public C blend(C src, C dst) =>
				// 参考：https://ofo.jp/osakana/cgtips/blendmode.phtml
				simpleBlend( dst.inv*src + src.inv*dst, src, dst );
		}
		sealed class Impl_Subtract : BlendMode {
			public Impl_Subtract() : base("fsub") {}
			override public C blend(C src, C dst) =>
				simpleBlend( dst-src, src, dst );
		}
		sealed class Impl_Divide : BlendMode {
			public Impl_Divide() : base("fdiv") {}
			override public C blend(C src, C dst) =>
				simpleBlend( dst/src, src, dst );
		}
		sealed class Impl_Hue : BlendMode {
			public Impl_Hue() : base("hue ") {}
			override public C blend(C src, C dst) {
				var ret = dst.hcy;
				ret.h = src.hcy.h;
				return simpleBlend( ret.rgb, src, dst );
			}
		}
		sealed class Impl_Saturation : BlendMode {
			public Impl_Saturation() : base("sat ") {}
			override public C blend(C src, C dst) {
				var ret = dst.hcy;
				ret.c = src.hcy.c;
				return simpleBlend( ret.rgb, src, dst );
			}
		}
		sealed class Impl_Color : BlendMode {
			public Impl_Color() : base("colr") {}
			override public C blend(C src, C dst) {
				var ret = dst.hcy;
				ret.h = src.hcy.h;
				ret.c = src.hcy.c;
				return simpleBlend( ret.rgb, src, dst );
			}
		}
		sealed class Impl_Luminosity : BlendMode {
			public Impl_Luminosity() : base("lum ") {}
			override public C blend(C src, C dst) {
				var ret = dst.hcy;
				ret.y = src.hcy.y;
				return simpleBlend( ret.rgb, src, dst );
			}
		}



		//---------------------------------------------------------------------------------------------------------------
	}
}
