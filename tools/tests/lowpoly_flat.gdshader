// Modèles low poly sans normales écrites (plan 60 §18, lot C3a) : l'ombrage par faces vient des dérivées de la
// position, et la couleur des sommets, écrite en sRGB, se prend telle quelle comme le faisait le StandardMaterial3D.
shader_type spatial;
render_mode diffuse_burley, specular_schlick_ggx;

#include "res://tools/tests/lowpoly_flat.gdshaderinc"
