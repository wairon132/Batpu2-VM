LDI r15 248
LDI r13 16
LDI r14 32
LDI r1 4  //color
LDI r2 0  //x
LDI r3 0  //y

.color_loop
STR r15 r1 2

.loop_y
.loop_x

STR r15 r2 -8
STR r15 r3 -7

ADD r1 r0 r0
BRH eq .clear_pixel
STR r15 r1 -6
JMP .skip_clear_pixel
.clear_pixel
STR r15 r1 -5
.skip_clear_pixel

ADI r2 1
SUB r2 r14 r0
BRH ne .loop_x

LDI r2 0
ADI r3 1
SUB r3 r14 r0

STR r15 r0 -3
BRH ne .loop_y

LDI r3 0
ADI r1 1
SUB r1 r13 r0
BRH ne .color_loop

LDI r1 0
JMP .color_loop
