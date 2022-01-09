
struct Euler
{
	float x;
	float y;
	float z;
}; typedef struct Euler euler;


euler eul_sum(euler a, euler b) {
	return (euler) { a.x + b.x, a.y + b.y, a.z + b.z };
}


__kernel void Euler2Color(__global euler* in, int width, int height, __global char* out)
{
	int x = get_global_id(0);
	int y = get_global_id(1);

	int inlinearId = (int)(x + y * width);
	euler eul = in[inlinearId];

	int4 col = convert_int4((float4)(255.0f * eul.x / 360.0f, 255.0f * eul.y / 90.0f, 255.0f * eul.z / 90.0f, 0));

	int outlinearId = (int)((x + y * width) * 4);
	out[outlinearId] = col.x; // R
	out[outlinearId + 1] = col.y; // G
	out[outlinearId + 2] = col.z; // B
	out[outlinearId + 3] = 255; // A
}


__kernel void Bc2Color(__global int* in, int width, int height, __global char* out)
{
	int x = get_global_id(0);
	int y = get_global_id(1);

	int inlinearId = (int)(x + y * width);
	int BC = in[inlinearId];

	int4 col = (int4)(BC, BC, BC, 0);

	int outlinearId = (int)((x + y * width) * 4);
	out[outlinearId] = col.x; // R
	out[outlinearId + 1] = col.y; // G
	out[outlinearId + 2] = col.z; // B
	out[outlinearId + 3] = 255; // A
}


__kernel void Extrapolate(__global euler* out, int width, int height, __global euler* in)
{
	int id = get_global_id(0);

	euler eul = in[id];

	if (eul.x > 0 && eul.y > 0 && eul.z > 0)
	{
		out[id] = eul;
	}
	else
	{
		int k = 0;
		euler sum = { 0, 0, 0 };

		bool can_up = id > width;
		bool can_left = (id % width) != 0;
		bool can_right = ((id + 1) % width) != 0;
		bool can_down = id < (width* height - width);

		bool can_upLeft = can_up && can_left;
		bool can_upRifht = can_up && can_right;
		bool can_downLeft = can_down && can_left;
		bool can_downRight = can_down && can_right;

		int up = id - width;
		int left = id - 1;
		int right = id + 1;
		int down = id + width;

		int upLeft = id - width - 1;
		int upRight = id - width + 1;
		int downLeft = id + width - 1;
		int downRight = id + width + 1;

		if (can_up && (in[up].x > 0 || in[up].y > 0 || in[up].z > 0)) { out[id] = in[up]; }
		if (can_left && (in[left].x > 0 || in[left].y > 0 || in[left].z > 0)) { out[id] = in[left]; }
		if (can_right && (in[right].x > 0 || in[right].y > 0 || in[right].z > 0)) { out[id] = in[right]; }
		if (can_down && (in[down].x > 0 || in[down].y > 0 || in[down].z > 0)) { out[id] = in[down]; }

		//if(can_up && (in[up].x > 0 || in[up].y > 0 || in[up].z >0)) {k++; sum = eul_sum(sum, in[up]); }    
		//if(can_left && (in[left].x > 0 || in[left].y > 0 || in[left].z >0)) {k++; sum = eul_sum(sum, in[left]); }    
		//if(can_right && (in[right].x > 0 || in[right].y > 0 || in[right].z >0)) {k++; sum = eul_sum(sum, in[right]); }    
		//if(can_down && (in[down].x > 0 || in[down].y > 0 || in[down].z >0)) {k++; sum = eul_sum(sum, in[down]); }    

		//if(can_upLeft && (in[upLeft].x > 0 || in[upLeft].y > 0 || in[upLeft].z >0)) {k++; sum = eul_sum(sum, in[upLeft]); }    
		//if(can_upRifht && (in[upRight].x > 0 || in[upRight].y > 0 || in[upRight].z >0)) {k++; sum = eul_sum(sum, in[upRight]); }    
		//if(can_downLeft && (in[downLeft].x > 0 || in[downLeft].y > 0 || in[downLeft].z >0)) {k++; sum = eul_sum(sum, in[downLeft]); }    
		//if(can_downRight && (in[downRight].x > 0 || in[downRight].y > 0 || in[downRight].z >0)) {k++; sum = eul_sum(sum, in[downRight]); }    

		//if(k >= 4){
		//    out[id] = (euler){ sum.x / k, sum.y / k, sum.z / k};
		//}

	}
}



float3 rotateVector(float3 a, euler eul)
{
	float3 angles = (float3)(radians(eul.x), radians(eul.y), radians(eul.z));
	a = (float3)(a.x * cos(angles.x) - a.y * sin(angles.x), a.x * sin(angles.x) + a.y * cos(angles.x), a.z); // Z - rotation
	a = (float3)(a.x * cos(angles.y) - a.z * sin(angles.y), a.y, -a.x * sin(angles.y) + a.z * cos(angles.y)); // Y - rotation
	a = (float3)(a.x, a.y * cos(angles.z) - a.z * sin(angles.z), a.y * sin(angles.z) + a.z * cos(angles.z)); // X - rotation
	return a;
}


float angleBetween(euler eul1, euler eul2) {

	float3 a = (float3)(1, 1, 1);
	float3 b = (float3)(1, 1, 1);

	a = rotateVector(a, eul1);
	b = rotateVector(b, eul2);

	return degrees(acos(dot(a, b) / (length(a) * length(b))));
}


__kernel void GetGrainMask(__global euler* in, int width, int height, float MissOrientationTreshold, __global char* out)
{
	int inId = get_global_id(0);

	euler eul = in[inId];

	bool isEdge = false;

	bool can_up = inId > width;
	bool can_left = inId % width != 0;
	bool can_right = (inId + 1) % width != 0;
	bool can_down = inId < width* height - width;

	int upId = inId - width;
	int leftId = inId - 1;
	int rightId = inId + 1;
	int downId = inId + width;

	if (can_up && isEdge == 0) { if (angleBetween(eul, in[upId]) > MissOrientationTreshold) isEdge = true; }
	if (can_left && isEdge == 0) { if (angleBetween(eul, in[leftId]) > MissOrientationTreshold) isEdge = true; }
	if (can_right && isEdge == 0) { if (angleBetween(eul, in[rightId]) > MissOrientationTreshold) isEdge = true; }
	if (can_down && isEdge == 0) { if (angleBetween(eul, in[downId]) > MissOrientationTreshold) isEdge = true; }

	if (isEdge) {
		int outId = inId * 4;
		out[outId] = 255;
		out[outId + 1] = 255;
		out[outId + 2] = 255;
		out[outId + 3] = 255;
	}
}


__kernel void ApplyMask(__global char* in, __global char* mask, __global char* out)
{
	int id = get_global_id(0);
	if (mask[id] == 0) out[id] = in[id];
	else out[id] = mask[id];
}

