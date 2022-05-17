
struct Euler
{
	float x;
	float y;
	float z;
}; typedef struct Euler euler;

bool isUnSolved(euler e) {
	return e.x == 0 && e.y == 0 && e.z == 0;
}

euler eul_sum(euler a, euler b) {
	return (euler) { a.x + b.x, a.y + b.y, a.z + b.z };
}

euler eul_subtract(euler a, euler b) {
	return (euler) { a.x - b.x, a.y - b.y, a.z - b.z };
}

int4 eulerToColor(euler eul) {
	return convert_int4((float4)(255.0f * eul.x / 360.0f, 255.0f * eul.y / 90.0f, 255.0f * eul.z / 90.0f, 0));
}

euler averageEuler(euler e1, euler e2, euler e3)
{
	euler sum = eul_sum(e3, eul_sum(e1, e2));
	euler av = (euler){ sum.x / 3.0, sum.y / 3.0, sum.z / 3.0 };
	return av;
}

float brightness(int4 col) {
	return 0.2126 * col.x + 0.7152 * col.y + 0.0722 * col.z;
}

float deviation(euler e1, euler e2, euler e3, euler M)
{
	int4 mCol = eulerToColor(M);
	int4 e1Col = eulerToColor(e1);
	int4 e2Col = eulerToColor(e2);
	int4 e3Col = eulerToColor(e3);
	float _e1 = (float)(brightness(e1Col) - brightness(mCol)) * (brightness(e1Col) - brightness(mCol));
	float _e2 = (float)(brightness(e2Col) - brightness(mCol)) * (brightness(e2Col) - brightness(mCol));
	float _e3 = (float)(brightness(e3Col) - brightness(mCol)) * (brightness(e3Col) - brightness(mCol));
	return (_e1 + _e2 + _e3) / 3.0;
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


__kernel void Euler2Color(__global euler* in, int width, int height, __global uchar* out)
{
	int x = get_global_id(0);
	int y = get_global_id(1);

	int inlinearId = (int)(x + y * width);
	euler eul = in[inlinearId];

	int4 col = eulerToColor(eul);

	int outlinearId = (int)((x + y * width) * 4);
	out[outlinearId] = col.z; // R
	out[outlinearId + 1] = col.y; // G
	out[outlinearId + 2] = col.x; // B
	out[outlinearId + 3] = 255; // A
}

__kernel void Bc2Color(__global int* in, int width, int height, __global uchar* out)
{
	int x = get_global_id(0);
	int y = get_global_id(1);

	int inlinearId = (int)(x + y * width);
	int BC = in[inlinearId];

	int4 col = (int4)(BC, BC, BC, 0);

	int outlinearId = (int)((x + y * width) * 4);
	out[outlinearId] = col.z; // R
	out[outlinearId + 1] = col.y; // G
	out[outlinearId + 2] = col.x; // B
	out[outlinearId + 3] = 255; // A
}

__kernel void Phase2Color(__global int* in, __global float4* phaseColors, int width, int height, __global uchar* out)
{
	int x = get_global_id(0);
	int y = get_global_id(1);

	int inlinearId = (int)(x + y * width);
	int phaseIndex = in[inlinearId];

	int4 col = (int4)(phaseColors[phaseIndex].x, phaseColors[phaseIndex].y, phaseColors[phaseIndex].z, phaseColors[phaseIndex].w);

	int outlinearId = (int)((x + y * width) * 4);
	out[outlinearId] = col.z; // R
	out[outlinearId + 1] = col.y; // G
	out[outlinearId + 2] = col.x; // B
	out[outlinearId + 3] = 255; // A
}

__kernel void ApplyMask(__global uchar* in, __global uchar* mask, int width, int height, __global uchar* out)
{
	int x = get_global_id(0);
	int y = get_global_id(1);

	int id = (int)(x + y * width) * 4;

	if (mask[id + 3] > 0 && mask[id + 3] < 255) {

		int R = convert_int(in[id] * (1.0f - (mask[id + 3] / 255.0f))) + convert_int(mask[id] * (mask[id + 3] / 255.0f));
		int G = convert_int(in[id + 1] * (1.0f - (mask[id + 3] / 255.0f))) + convert_int(mask[id + 1] * (mask[id + 3] / 255.0f));
		int B = convert_int(in[id + 2] * (1.0f - (mask[id + 3] / 255.0f))) + convert_int(mask[id + 2] * (mask[id + 3] / 255.0f));

		out[id] = R;
		out[id + 1] = G;
		out[id + 2] = B;
		out[id + 3] = 255;
	}
	else {
		if (mask[id] == 0 && mask[id + 1] == 0 && mask[id + 2] == 0 && mask[id + 3] == 0) {
			out[id] = in[id];
			out[id + 1] = in[id + 1];
			out[id + 2] = in[id + 2];
			out[id + 3] = in[id + 3];
		}
		else {
			out[id] = convert_int(mask[id]);
			out[id + 1] = convert_int(mask[id + 1]);
			out[id + 2] = convert_int(mask[id + 2]);
			out[id + 3] = convert_int(mask[id + 3]);
		}
	}
}


__kernel void GetGrainMask(__global euler* in, int width, int height, float MissOrientationTreshold, float4 grainMaskColor, __global uchar* out)
{
	int x = get_global_id(0);
	int y = get_global_id(1);
	int id = (int)(x + y * width);

	int idUp = (int)(x + (y + 1) * width);
	int idDown = (int)(x + (y - 1) * width);
	int idLeft = (int)((x - 1) + y * width);
	int idRight = (int)((x + 1) + y * width);

	bool isEdge = false;

	if (!isEdge && y > 1) { if (angleBetween(in[id], in[idUp]) > MissOrientationTreshold) isEdge = true; }
	if (!isEdge && y < height - 1) { if (angleBetween(in[id], in[idDown]) > MissOrientationTreshold) isEdge = true; }
	if (!isEdge && x > 1) { if (angleBetween(in[id], in[idLeft]) > MissOrientationTreshold) isEdge = true; }
	if (!isEdge && x < width - 1) { if (angleBetween(in[id], in[idRight]) > MissOrientationTreshold) isEdge = true; }

	int outId = id * 4;
	if (isEdge) {
		out[outId] = convert_int(grainMaskColor.z);
		out[outId + 1] = convert_int(grainMaskColor.y);
		out[outId + 2] = convert_int(grainMaskColor.x);
		out[outId + 3] = convert_int(grainMaskColor.w);
	}
}

__kernel void GetStrainMaskKAM(__global euler* in, int width, int height, float4 lowCol, float4 highCol, float referenceDeviation, int opacity, __global uchar* out)
{
	int x = get_global_id(0);
	int y = get_global_id(1);
	int id = (int)(x + y * width);
	int outId = id * 4;

	int idUp = (int)(x + (y + 1) * width);
	int idDown = (int)(x + (y - 1) * width);
	int idLeft = (int)((x - 1) + y * width);
	int idRight = (int)((x + 1) + y * width);

	int idUpLeft = (int)((x - 1) + (y + 1) * width);
	int idDownLeft = (int)((x - 1) + (y - 1) * width);
	int idUpRight = (int)((x + 1) + (y + 1) * width);
	int idDownRight = (int)((x + 1) + (y - 1) * width);

	float upDeviation = 0.0f;
	float downDeviation = 0.0f;
	float leftDeviation = 0.0f;
	float rightDeviation = 0.0f;

	float upLeftDeviation = 0.0f;
	float downLeftDeviation = 0.0f;
	float upRightDeviation = 0.0f;
	float downRightDeviation = 0.0f;

	float n = 0.0f;
	float max = referenceDeviation;

	if (y > 0) { n = n + 1.0f; upDeviation = angleBetween(in[id], in[idUp]); if (upDeviation > max) max = upDeviation; }
	if (y < height) { n = n + 1.0f; downDeviation = angleBetween(in[id], in[idDown]); if (downDeviation > max) max = downDeviation; }
	if (x > 0) { n = n + 1.0f; leftDeviation = angleBetween(in[id], in[idLeft]); if (leftDeviation > max) max = leftDeviation; }
	if (x < width) { n = n + 1.0f; rightDeviation = angleBetween(in[id], in[idRight]); if (rightDeviation > max) max = rightDeviation; }

	if (y > 0 && x > 0) { n = n + 1.0f; upLeftDeviation = angleBetween(in[id], in[idUpLeft]); if (upLeftDeviation > max) max = upLeftDeviation; }
	if (y < height && x > 0) { n = n + 1.0f; downLeftDeviation = angleBetween(in[id], in[idDownLeft]); if (downLeftDeviation > max) max = downLeftDeviation; }
	if (y > 0 && x < width) { n = n + 1.0f; upRightDeviation = angleBetween(in[id], in[idUpRight]); if (upRightDeviation > max) max = upRightDeviation; }
	if (y < height && x < width) { n = n + 1.0f; downRightDeviation = angleBetween(in[id], in[idDownRight]); if (downRightDeviation > max) max = downRightDeviation; }

	float averageDeviation =
		(upDeviation +
			downDeviation +
			leftDeviation +
			rightDeviation +
			upLeftDeviation +
			downLeftDeviation +
			upRightDeviation +
			downRightDeviation) / n;

	float t = averageDeviation / max;  // referenceDeviation max
	int R = convert_int(lowCol.x * (1.0f - t) + convert_int(highCol.x * t));
	int G = convert_int(lowCol.y * (1.0f - t) + convert_int(highCol.y * t));
	int B = convert_int(lowCol.z * (1.0f - t) + convert_int(highCol.z * t));


	//---------------

	out[outId] = B; //r
	out[outId + 1] = G; //g
	out[outId + 2] = R; //b
	out[outId + 3] = opacity;
}


__kernel void StandartCleanUp(__global euler* in, int width, int height, __global euler* out)
{
	int x = get_global_id(0);
	int y = get_global_id(1);
	int id = (int)(x + y * width);

	int idUp = (int)(x + (y + 1) * width);
	int idDown = (int)(x + (y - 1) * width);
	int idLeft = (int)((x - 1) + y * width);
	int idRight = (int)((x + 1) + y * width);

	int idUpLeft = (int)((x - 1) + (y + 1) * width);
	int idDownLeft = (int)((x - 1) + (y - 1) * width);
	int idUpRight = (int)((x + 1) + (y + 1) * width);
	int idDownRight = (int)((x + 1) + (y - 1) * width);

	euler eul = in[id];
	if (eul.x != 0 || eul.y != 0 || eul.z != 0) out[id] = eul;
	else {

		euler sum = (euler){ 0,0,0 };

		//Standart ----------------
		int k = 0;

		if (in[idUp].x != 0 || in[idUp].y != 0 || in[idUp].z != 0) {
			sum = eul_sum(sum, in[idUp]);
			k++;
		}
		if (in[idDown].x != 0 || in[idDown].y != 0 || in[idDown].z != 0) {
			sum = eul_sum(sum, in[idDown]);
			k++;
		}
		if (in[idLeft].x != 0 || in[idLeft].y != 0 || in[idLeft].z != 0) {
			sum = eul_sum(sum, in[idLeft]);
			k++;
		}
		if (in[idRight].x != 0 || in[idRight].y != 0 || in[idRight].z != 0) {
			sum = eul_sum(sum, in[idRight]);
			k++;
		}

		if (in[idUpLeft].x != 0 || in[idUpLeft].y != 0 || in[idUpLeft].z != 0) {
			sum = eul_sum(sum, in[idUpLeft]);
			k++;
		}
		if (in[idDownLeft].x != 0 || in[idDownLeft].y != 0 || in[idDownLeft].z != 0) {
			sum = eul_sum(sum, in[idDownLeft]);
			k++;
		}
		if (in[idUpRight].x != 0 || in[idUpRight].y != 0 || in[idUpRight].z != 0) {
			sum = eul_sum(sum, in[idUpRight]);
			k++;
		}
		if (in[idDownRight].x != 0 || in[idDownRight].y != 0 || in[idDownRight].z != 0) {
			sum = eul_sum(sum, in[idDownRight]);
			k++;
		}

		if (k > 0) {
			out[id] = (euler){ sum.x / k, sum.y / k, sum.z / k };
		}

	}
}


__kernel void KuwaharaCleanUp(__global euler* in, int width, int height, __global euler* out)
{
	int x = get_global_id(0);
	int y = get_global_id(1);
	int id = (int)(x + y * width);

	int idUp = (int)(x + (y + 1) * width);
	int idDown = (int)(x + (y - 1) * width);
	int idLeft = (int)((x - 1) + y * width);
	int idRight = (int)((x + 1) + y * width);

	int idUpLeft = (int)((x - 1) + (y + 1) * width);
	int idDownLeft = (int)((x - 1) + (y - 1) * width);
	int idUpRight = (int)((x + 1) + (y + 1) * width);
	int idDownRight = (int)((x + 1) + (y - 1) * width);

	euler eul = in[id];
	if (eul.x != 0 || eul.y != 0 || eul.z != 0) out[id] = eul;
	else {

		euler average = (euler){ 0,0,0 };

		//Kuwahara ----------------
		float minD = 10000;

		if (x > 0 && y > 0 && !isUnSolved(in[idUp]) && !isUnSolved(in[idLeft]) && !isUnSolved(in[idUpLeft])) {
			euler M1 = averageEuler(in[idUp], in[idLeft], in[idUpLeft]);
			float D1 = deviation(in[idUp], in[idLeft], in[idUpLeft], M1);
			if (D1 < minD) {
				minD = D1;
				average = M1;
			}
		}
		if (x < width && y > 0 && !isUnSolved(in[idUp]) && !isUnSolved(in[idRight]) && !isUnSolved(in[idUpRight])) {
			euler M2 = averageEuler(in[idUp], in[idRight], in[idUpRight]);
			float D2 = deviation(in[idUp], in[idRight], in[idUpRight], M2);
			if (D2 < minD) {
				minD = D2;
				average = M2;
			}
		}
		if (x < width && y < height && !isUnSolved(in[idDown]) && !isUnSolved(in[idRight]) && !isUnSolved(in[idDownRight])) {
			euler M3 = averageEuler(in[idDown], in[idRight], in[idDownRight]);
			float D3 = deviation(in[idDown], in[idRight], in[idDownRight], M3);
			if (D3 < minD) {
				minD = D3;
				average = M3;
			}
		}
		if (x > 0 && y < height && !isUnSolved(in[idDown]) && !isUnSolved(in[idLeft]) && !isUnSolved(in[idDownLeft])) {
			euler M4 = averageEuler(in[idDown], in[idLeft], in[idDownLeft]);
			float D4 = deviation(in[idDown], in[idLeft], in[idDownLeft], M4);
			if (D4 < minD) {
				minD = D4;
				average = M4;
			}
		}

		out[id] = average;
	}
}

