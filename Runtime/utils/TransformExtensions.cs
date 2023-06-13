/**
 * @copyright Copyright (c) 2021-2022 B-com http://www.b-com.com/
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

// https://forum.unity.com/threads/how-to-assign-matrix4x4-to-transform.121966/#post-819280

using UnityEngine;

public static class TransformExtensions
{
	public static void FromMatrix(this Transform transform, Matrix4x4 matrix)
	{
		transform.localScale = matrix.ExtractScale();
		transform.rotation = matrix.ExtractRotation();
		transform.position = matrix.ExtractPosition();
	}
}