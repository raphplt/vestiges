void fragment() {
	// Le sens du produit dépend de la convention d'axes de l'image : on tourne la face vers la caméra.
	vec3 face = normalize(cross(dFdx(VERTEX), dFdy(VERTEX)));
	NORMAL = dot(face, VERTEX) > 0.0 ? -face : face;
	ALBEDO = COLOR.rgb;
	ROUGHNESS = 1.0;
}
