#if defined(CORTAO_STEREO_INPUT)
Texture2DArray<float> _Depth;
float ReadDepth(int2 p) { return _Depth.Load(int4(p,_CORTAOEye,0)); }
#else
Texture2D<float> _Depth;
float ReadDepth(int2 p) { return _Depth.Load(int3(p,0)); }
#endif
