;ASM Library that contains procedure to filter properly formated image with given 3x3 filter
;autor Mateusz Czarnecki

.code

;========================= ARGUMENTS ==============================
;RCX - pointer to array with filter [IntPtr 32bit]
;RDX - pointer to array with part of the image [IntPtr 32bit]
;R8 - length of array with part of the image [int 32bit]
;XMM3 - norm [float 32bit]
;==================================================================

;=========== Register Description =================================
;RCX - pointer to array with filter
;RDX - pointer to array with part of the image
;R8 - length of array with part of the image

;R14 - index of color array
;R15 - index of filter array

;XMM0 - sum RGB values of current part of the image |x|R|G|B|
;XMM1 - currelt pixel |x|R|G|B|
;XMM2 - current filter |x|filter|filter|filter|
;XMM3 - norm |x|norm|norm|norm|
;==================================================================


filterProc PROC
				
				PUSH R14
				PUSH R15

				XOR R14, R14							; set index of color array to 0
				XOR R15, R15							; set index of filter array to 0
				XORPS XMM0, XMM0						; set all bits of XMM0 register to 0

				UNPCKLPS XMM3, XMM3						; unpack norm in the register |x|norm|x|norm|
				UNPCKLPS XMM3, XMM3						; unpack norm in the register |norm|norm|norm|norm|

			filterLoop:
				MOVUPS XMM1, [RDX + R14]				; move current pixel to XMM1 register |x|R|G|B|
				
				MOVUPS XMM2, [RCX + R15]				; move filter to XMM1 register
				UNPCKLPS XMM2, XMM2						; unpack current filter value in the register |x|filter|x|filter|
				UNPCKLPS XMM2, XMM2						; unpack current filter value in the register |filter|filter|filter|filter|
				
				MULPS XMM1, XMM2						; multiply current pixel by filter 
				ADDPS XMM0, XMM1						; add multiplied values to sum of all pixels
				ADD R14, 12								; move index of color array to the next pixel
				ADD R15, 4								; move index of filter array to next filter element
				SUB R8, 3								; decrement filter loop counter
				CMP R8, 0								; check end of the loop
				JNZ filterLoop							; if it is not the end, loop again

				DIVPS XMM0, XMM3						; divide sum of all pixels by norm

				MOVAPS [RDX], XMM0						; store result in memory

				POP R15
				POP R14

				RET										; return from procedure

filterProc endp

end
