using System;
using UnityEngine;


namespace PsdSerializer {

	/** AdobeのHCY(Y240)でカラーを表現する形式 */
	public struct HCY {
		public double h, c, y, a;
		public HCY(double h, double c, double y, double a) {this.h=h; this.c=c; this.y=y; this.a=a;}

		public C rgb {get{
			// 参考：https://en.wikipedia.org/wiki/HSL_and_HSV
			var hDash = h / 0.16666666666666;
			var x = c*(1.0 - Math.Abs(hDash%2 - 1));

			double rr, gg, bb;
			if (hDash < 1)		(rr,gg,bb) = (c,x,0);
			else if (hDash < 2)	(rr,gg,bb) = (x,c,0);
			else if (hDash < 3)	(rr,gg,bb) = (0,c,x);
			else if (hDash < 4)	(rr,gg,bb) = (0,x,c);
			else if (hDash < 5)	(rr,gg,bb) = (x,0,c);
			else				(rr,gg,bb) = (c,0,x);
			
			var m = y - (0.212*rr + 0.701*gg + 0.087*bb);
			
			return new C( (float)(rr+m), (float)(gg+m), (float)(bb+m), (float)a );
		}}
	}


	/** カラーを扱うための構造体。Colorより便利 */
	public struct C {
		//----------------------------------------------- publicフィールド ---------------------------------------------

		public float r, g, b, a;

		public C inv => new C(-r, -g, -b, -a);
		public C sqrt => new C(Mathf.Sqrt(r), Mathf.Sqrt(g), Mathf.Sqrt(b), Mathf.Sqrt(a));
		public C abs => new C(Mathf.Abs(r), Mathf.Abs(g), Mathf.Abs(b), Mathf.Abs(a));

		public HCY hcy {get{
			// 参考：https://en.wikipedia.org/wiki/HSL_and_HSV
			double h, t, max, min;
			if (r<g) {
				if (g<b) {max=b; min=r; t=4; h=r-g;}
				else {max=g; min=r<b?r:b; t=2; h=b-r;}
			} else {
				if (r<b) {max=b; min=g; t=4; h=r-g;}
				else {max=r; min=g<b?g:b; t=0; h=g-b;}
			}
			var c = max-min;
			h = c<0.00000001 ? 0 : ( 1.0 + 0.16666666666666 * (t + h/c) )%1;

			var y = 0.212*r + 0.701*g + 0.087*b;

			return new HCY( h, c, y, a );
		}}

		public C( float r, float g, float b, float a ) 
			{this.r=r; this.g=g; this.b=b; this.a=a;}
		public C( float v ) {r=v; g=v; b=v; a=v;}

		public C branch(C one, C zero) => new C(
			r < 0.5 ? zero.r : one.r,
			g < 0.5 ? zero.g : one.g,
			b < 0.5 ? zero.b : one.b,
			a < 0.5 ? zero.a : one.a
		);

		static public implicit operator C(Color a) => new C(a.r, a.g, a.b, a.a);
		static public implicit operator Color(C a) => new Color(
			Mathf.Clamp01(a.r), Mathf.Clamp01(a.g), Mathf.Clamp01(a.b), Mathf.Clamp01(a.a)
		);

		static public C operator+ (C a, C b) => new C( a.r+b.r, a.g+b.g, a.b+b.b, a.a+b.a );
		static public C operator+ (C a, float b) => new C( a.r+b, a.g+b, a.b+b, a.a+b );
		static public C operator+ (float a, C b) => b+a;
		static public C operator- (C a) => new C(-a.r, -a.g, -a.b, -a.a);
		static public C operator- (C a, C b) => new C( a.r-b.r, a.g-b.g, a.b-b.b, a.a-b.a );
		static public C operator- (C a, float b) => a + -b;
		static public C operator- (float a, C b) => a + -b;
		static public C operator* (C a, C b) => new C( a.r*b.r, a.g*b.g, a.b*b.b, a.a*b.a );
		static public C operator* (C a, float b) => new C( a.r*b, a.g*b, a.b*b, a.a*b );
		static public C operator* (float a, C b) => b*a;
		static public C operator/ (C a, C b) => new C( a.r/b.r, a.g/b.g, a.b/b.b, a.a/b.a );
		static public C operator/ (C a, float b) => new C( a.r/b, a.g/b, a.b/b, a.a/b );
		static public C operator< (C a, C b) => new C(
			a.r<b.r?1:0, a.g<b.g?1:0, a.b<b.b?1:0, a.a<b.a?1:0
		);
		static public C operator< (C a, float b) => new C(
			a.r<b?1:0, a.g<b?1:0, a.b<b?1:0, a.a<b?1:0
		);
		static public C operator< (float a, C b) => b > a;
		static public C operator<= (C a, C b) => new C(
			a.r<=b.r?1:0, a.g<=b.g?1:0, a.b<=b.b?1:0, a.a<=b.a?1:0
		);
		static public C operator<= (C a, float b) => new C(
			a.r<=b?1:0, a.g<=b?1:0, a.b<=b?1:0, a.a<=b?1:0
		);
		static public C operator<= (float a, C b) => b >= a;
		static public C operator> (C a, C b) => new C(
			a.r>b.r?1:0, a.g>b.g?1:0, a.b>b.b?1:0, a.a>b.a?1:0
		);
		static public C operator> (C a, float b) => new C(
			a.r>b?1:0, a.g>b?1:0, a.b>b?1:0, a.a>b?1:0
		);
		static public C operator> (float a, C b) => b < a;
		static public C operator>= (C a, C b) => new C(
			a.r>=b.r?1:0, a.g>=b.g?1:0, a.b>=b.b?1:0, a.a>=b.a?1:0
		);
		static public C operator>= (C a, float b) => new C(
			a.r>=b?1:0, a.g>=b?1:0, a.b>=b?1:0, a.a>=b?1:0
		);
		static public C operator>= (float a, C b) => b <= a;
		static public C operator== (C a, C b) => new C(
			a.r==b.r?1:0, a.g==b.g?1:0, a.b==b.b?1:0, a.a==b.a?1:0
		);
		static public C operator!= (C a, C b) => new C(
			a.r!=b.r?1:0, a.g!=b.g?1:0, a.b!=b.b?1:0, a.a!=b.a?1:0
		);
		
		static public C min(C a, C b) => new C(
			Mathf.Min(a.r, b.r), Mathf.Min(a.g, b.g), Mathf.Min(a.b, b.b), Mathf.Min(a.a, b.a)
		);
		static public C max(C a, C b) => new C(
			Mathf.Max(a.r, b.r), Mathf.Max(a.g, b.g), Mathf.Max(a.b, b.b), Mathf.Max(a.a, b.a)
		);



		public override bool Equals(object obj) {
			if (obj == null) return false;
			var c = (C)obj;
			return c.r==r && c.g==g && c.b==b && c.a==a;
		}
		public override int GetHashCode() =>
			r.GetHashCode()^g.GetHashCode()^b.GetHashCode()^a.GetHashCode();



		//----------------------------------------- private / protected フィールド -------------------------------------

		//---------------------------------------------------------------------------------------------------------------
	}
}
