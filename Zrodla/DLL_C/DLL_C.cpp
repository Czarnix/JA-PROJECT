#include "pch.h" // use stdafx.h in Visual Studio 2017 and earlier
#include <utility>
#include <limits.h>
#include "DLL_C.h"
#include <xmmintrin.h>

void filterImage(float* filterPointer, float* arrayPointer, int length, float filterNorm) 
{

	__m128 XMM0; // sum of RGB |x|R|G|B|
	__m128 XMM1; // currently processed pixel |x|R|G|B|
	__m128 XMM2; // current filter |x|filter|filter|filter|
	__m128 XMM3; // norm |x|norm|norm|norm|
	
	XMM3 = _mm_load_ss(&filterNorm); // loading norm to the register |0|0|0|norm|
	XMM3 = _mm_unpacklo_ps(XMM3, XMM3); // unpacking norm |0|norm|0|norm|
	XMM3 = _mm_unpacklo_ps(XMM3, XMM3); // second norm unpacking |norm|norm|norm|norm|

	XMM0 = _mm_setzero_ps(); // set all bits of XMM0 register to 0
	int offset = 0; // setting ofset
	while (length != 0) 
	{
		XMM1 = _mm_loadu_ps(arrayPointer + (__int64)offset * 3); // moving currently processed pixel (R,G,B) to XMM1 |x|R|G|B|
		XMM2 = _mm_loadu_ps(filterPointer + offset); // loading filter to the register 
		XMM2 = _mm_unpacklo_ps(XMM2, XMM2); // unpacking filter
		XMM2 = _mm_unpacklo_ps(XMM2, XMM2);

		XMM1 = _mm_mul_ps(XMM1, XMM2); // multiplying current RGB values by filter value
		XMM0 = _mm_add_ps(XMM0, XMM1); // adding current multiplied RGB values to sum of all colors

		offset++; // moving memory pointer
		length -= 3; // decrementing loop counter
	}

	XMM0 = _mm_div_ps(XMM0, XMM3); // dividing sum of colors by norm
	_mm_store_ps(arrayPointer, XMM0); // moving calculated colors back to memory
}
