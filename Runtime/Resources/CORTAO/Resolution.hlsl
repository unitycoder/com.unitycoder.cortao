#ifndef CORTAO_RESOLUTION_INCLUDED
#define CORTAO_RESOLUTION_INCLUDED
int _FullWidth, _FullHeight;
int2 FullSize() { return int2(_FullWidth, _FullHeight); }
int2 TraceSize() { return int2(_PackedResolution & 65535, (uint)_PackedResolution >> 16); }
int2 FullPixel(int2 p) { return clamp((int2)floor((float2(p)+0.5)*FullSize()/TraceSize()),0,FullSize()-1); }
#endif
