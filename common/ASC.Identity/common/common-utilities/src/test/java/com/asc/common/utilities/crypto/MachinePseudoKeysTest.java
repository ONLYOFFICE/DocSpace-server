// (c) Copyright Ascensio System SIA 2009-2025
//
// This program is a free software product.
// You can redistribute it and/or modify it under the terms
// of the GNU Affero General Public License (AGPL) version 3 as published by the Free Software
// Foundation. In accordance with Section 7(a) of the GNU AGPL its Section 15 shall be amended
// to the effect that Ascensio System SIA expressly excludes the warranty of non-infringement of
// any third-party rights.
//
// This program is distributed WITHOUT ANY WARRANTY, without even the implied warranty
// of MERCHANTABILITY or FITNESS FOR A PARTICULAR  PURPOSE. For details, see
// the GNU AGPL at: http://www.gnu.org/licenses/agpl-3.0.html
//
// You can contact Ascensio System SIA at Lubanas st. 125a-25, Riga, Latvia, EU, LV-1021.
//
// The  interactive user interfaces in modified source and object code versions of the Program must
// display Appropriate Legal Notices, as required under Section 5 of the GNU AGPL version 3.
//
// Pursuant to Section 7(b) of the License you must retain the original Product logo when
// distributing the program. Pursuant to Section 7(e) we decline to grant you any rights under
// trademark law for use of our trademarks.
//
// All the Product's GUI elements, including illustrations and icon sets, as well as technical
// writing
// content are licensed under the terms of the Creative Commons Attribution-ShareAlike 4.0
// International. See the License terms at http://creativecommons.org/licenses/by-sa/4.0/legalcode

package com.asc.common.utilities.crypto;

import static org.junit.jupiter.api.Assertions.assertArrayEquals;
import static org.junit.jupiter.api.Assertions.assertEquals;

import java.util.stream.Stream;
import org.junit.jupiter.params.ParameterizedTest;
import org.junit.jupiter.params.provider.Arguments;
import org.junit.jupiter.params.provider.MethodSource;

public class MachinePseudoKeysTest {

  @ParameterizedTest(name = "Machine Pseudo Keys with Secret: {0}")
  @MethodSource("machineKeyProvider")
  public void whenSecretProvided_thenGetMachineConstant256MatchesExpectedIntArray(
      String secret, int[] expectedInts) {
    var machinePseudoKeys = new MachinePseudoKeys(secret);
    var actualBytes = machinePseudoKeys.getMachineConstant(256);

    assertEquals(
        256, actualBytes.length, "Generated machine constant should be 256 bytes in length");

    var actualInts = toIntArray(actualBytes);

    assertArrayEquals(
        expectedInts,
        actualInts,
        "The generated machine constant does not match the expected value");
  }

  private static Stream<Arguments> machineKeyProvider() {
    return Stream.of(
        Arguments.of(
            "111",
            new int[] {
              156, 182, 160, 235, 211, 5, 157, 158, 161, 51, 67, 144, 220, 176, 14, 216, 171, 168,
              66, 92, 213, 102, 130, 34, 84, 107, 164, 117, 134, 186, 242, 250, 116, 205, 2, 29, 50,
              72, 229, 101, 240, 14, 83, 205, 56, 241, 6, 148, 120, 235, 179, 223, 132, 181, 53, 52,
              125, 151, 104, 96, 39, 23, 230, 65, 73, 27, 14, 174, 241, 165, 98, 195, 220, 108, 198,
              19, 180, 234, 98, 100, 16, 253, 155, 6, 19, 118, 190, 152, 205, 160, 155, 224, 132,
              62, 216, 39, 17, 132, 28, 227, 88, 163, 210, 136, 240, 2, 23, 239, 33, 127, 147, 52,
              3, 79, 41, 124, 223, 46, 211, 93, 117, 224, 80, 9, 130, 62, 158, 148, 158, 2, 48, 205,
              126, 12, 109, 42, 18, 22, 16, 94, 207, 118, 77, 12, 102, 221, 53, 20, 92, 72, 227,
              177, 191, 110, 119, 82, 201, 6, 177, 99, 131, 80, 31, 79, 197, 181, 246, 226, 255,
              105, 201, 29, 116, 141, 254, 147, 67, 163, 165, 9, 138, 56, 86, 31, 126, 14, 16, 149,
              27, 97, 12, 101, 173, 218, 126, 87, 254, 71, 176, 230, 82, 21, 242, 126, 197, 60, 49,
              111, 227, 14, 37, 97, 167, 217, 75, 250, 0, 209, 183, 164, 97, 198, 157, 93, 99, 112,
              154, 15, 167, 149, 252, 242, 190, 182, 116, 69, 215, 89, 66, 221, 161, 177, 12, 60,
              170, 190, 212, 143, 132, 86, 44, 144, 11, 133, 140, 119
            }),
        Arguments.of(
            "222",
            new int[] {
              138, 154, 36, 248, 109, 97, 156, 240, 82, 113, 250, 27, 83, 216, 78, 242, 90, 70, 77,
              187, 200, 214, 219, 166, 107, 92, 41, 83, 104, 192, 65, 78, 101, 2, 142, 212, 64, 149,
              80, 58, 161, 140, 6, 187, 210, 247, 133, 48, 45, 40, 143, 97, 191, 239, 179, 191, 126,
              141, 17, 55, 72, 136, 146, 47, 172, 182, 81, 74, 122, 178, 197, 245, 18, 26, 59, 208,
              31, 211, 116, 214, 248, 38, 63, 49, 224, 142, 118, 79, 206, 85, 178, 132, 24, 242, 24,
              250, 214, 15, 28, 165, 59, 182, 123, 99, 153, 78, 164, 179, 226, 160, 171, 25, 58, 63,
              33, 72, 96, 79, 29, 63, 2, 124, 36, 255, 64, 221, 32, 1, 65, 249, 16, 182, 206, 155,
              65, 171, 220, 151, 145, 233, 195, 108, 46, 170, 153, 73, 216, 208, 208, 153, 135, 241,
              220, 163, 190, 145, 123, 35, 188, 46, 162, 114, 232, 143, 244, 74, 159, 254, 118, 108,
              200, 189, 51, 124, 150, 77, 122, 102, 247, 4, 79, 49, 167, 114, 30, 218, 43, 220, 175,
              47, 185, 219, 99, 70, 80, 131, 158, 182, 78, 170, 218, 89, 100, 209, 202, 190, 95, 13,
              113, 23, 21, 43, 183, 223, 113, 203, 118, 113, 25, 31, 194, 78, 70, 178, 236, 89, 236,
              43, 18, 175, 195, 23, 76, 41, 245, 204, 214, 168, 96, 122, 29, 107, 152, 26, 141, 36,
              132, 213, 133, 13, 45, 156, 47, 231, 140, 19
            }),
        Arguments.of(
            "test",
            new int[] {
              250, 100, 177, 47, 210, 140, 143, 113, 64, 171, 243, 164, 66, 173, 201, 254, 194, 108,
              110, 51, 79, 188, 162, 223, 50, 36, 154, 245, 78, 25, 38, 26, 240, 67, 55, 16, 74,
              136, 73, 97, 16, 198, 85, 242, 49, 10, 198, 214, 5, 27, 136, 119, 111, 130, 61, 194,
              210, 252, 174, 241, 153, 34, 39, 133, 217, 179, 254, 118, 184, 179, 57, 34, 13, 35,
              136, 102, 175, 173, 40, 93, 195, 240, 51, 144, 174, 171, 110, 5, 117, 62, 78, 218, 87,
              199, 237, 159, 208, 25, 126, 12, 80, 30, 81, 225, 101, 106, 76, 249, 215, 18, 36, 212,
              80, 45, 168, 239, 150, 215, 46, 69, 248, 1, 121, 100, 95, 203, 70, 53, 233, 150, 150,
              47, 27, 13, 165, 158, 81, 43, 68, 94, 116, 46, 98, 26, 122, 137, 41, 31, 253, 8, 249,
              235, 56, 19, 79, 164, 237, 129, 154, 35, 22, 16, 65, 123, 244, 184, 67, 136, 9, 158,
              107, 147, 207, 209, 202, 158, 95, 234, 214, 161, 38, 56, 224, 156, 170, 247, 8, 189,
              1, 177, 207, 144, 32, 72, 99, 236, 230, 37, 193, 69, 161, 21, 95, 157, 102, 28, 103,
              73, 177, 69, 2, 171, 249, 253, 222, 47, 164, 209, 253, 176, 134, 135, 87, 206, 219,
              115, 134, 109, 221, 183, 57, 40, 144, 255, 17, 216, 67, 54, 142, 143, 190, 12, 187,
              175, 35, 151, 35, 105, 52, 71, 21, 40, 17, 191, 26, 189
            }),
        Arguments.of(
            "mock",
            new int[] {
              43, 205, 254, 196, 181, 251, 56, 126, 89, 227, 30, 112, 21, 70, 88, 225, 36, 107, 11,
              155, 38, 33, 101, 103, 10, 74, 216, 149, 207, 163, 76, 33, 61, 69, 40, 220, 193, 55,
              148, 21, 215, 131, 243, 163, 68, 166, 64, 90, 206, 152, 163, 76, 242, 31, 9, 104, 151,
              186, 107, 35, 163, 175, 181, 151, 253, 51, 208, 30, 124, 31, 236, 214, 245, 196, 163,
              46, 194, 35, 99, 9, 125, 198, 54, 255, 255, 47, 30, 59, 192, 69, 7, 204, 113, 114, 40,
              205, 91, 165, 16, 214, 34, 222, 175, 172, 205, 87, 45, 123, 219, 165, 116, 86, 97,
              166, 220, 120, 182, 152, 205, 20, 148, 94, 55, 23, 32, 101, 130, 155, 214, 210, 28,
              18, 140, 231, 158, 23, 138, 49, 168, 1, 163, 95, 26, 208, 176, 107, 202, 150, 175, 22,
              195, 216, 252, 65, 196, 167, 151, 139, 104, 212, 146, 165, 8, 137, 98, 201, 121, 8,
              197, 238, 132, 239, 204, 113, 52, 67, 102, 102, 180, 154, 236, 236, 191, 15, 68, 21,
              75, 35, 247, 127, 254, 201, 211, 111, 253, 87, 145, 110, 231, 242, 194, 209, 193, 146,
              211, 12, 139, 12, 128, 64, 49, 215, 205, 232, 165, 230, 249, 68, 76, 126, 85, 16, 69,
              240, 187, 27, 92, 115, 221, 178, 248, 127, 194, 215, 26, 42, 44, 59, 56, 137, 62, 162,
              182, 77, 39, 251, 234, 201, 23, 94, 76, 33, 104, 156, 177, 139
            }),
        Arguments.of(
            "secret",
            new int[] {
              129, 160, 90, 180, 118, 156, 150, 105, 28, 239, 224, 56, 178, 28, 28, 13, 228, 13, 39,
              248, 54, 29, 211, 136, 131, 213, 31, 115, 162, 196, 191, 178, 123, 184, 122, 30, 240,
              93, 14, 46, 173, 62, 113, 169, 207, 198, 233, 23, 127, 81, 84, 134, 210, 92, 100, 205,
              209, 48, 160, 124, 34, 199, 88, 48, 46, 188, 250, 162, 254, 28, 134, 254, 248, 75,
              247, 171, 42, 185, 189, 236, 7, 244, 80, 111, 57, 224, 30, 83, 173, 76, 192, 188, 145,
              12, 229, 230, 65, 123, 18, 203, 71, 25, 98, 202, 85, 141, 134, 101, 185, 162, 23, 114,
              180, 116, 46, 118, 233, 247, 77, 158, 166, 245, 178, 92, 202, 109, 236, 101, 17, 105,
              174, 167, 241, 165, 238, 145, 133, 25, 171, 89, 185, 154, 10, 52, 77, 8, 28, 222, 110,
              252, 74, 45, 115, 37, 81, 102, 6, 0, 232, 160, 33, 84, 79, 243, 112, 129, 15, 134,
              156, 241, 207, 75, 243, 229, 12, 235, 125, 14, 194, 80, 14, 246, 20, 31, 129, 51, 204,
              83, 135, 138, 133, 48, 10, 56, 100, 74, 22, 196, 203, 249, 150, 65, 125, 44, 254, 58,
              142, 24, 102, 233, 247, 62, 152, 146, 42, 64, 48, 114, 60, 181, 187, 254, 18, 107,
              159, 64, 186, 128, 194, 212, 185, 66, 200, 185, 205, 121, 231, 32, 70, 164, 179, 236,
              158, 147, 70, 152, 120, 14, 36, 25, 163, 135, 22, 61, 152, 47
            }),
        Arguments.of(
            "supersecret",
            new int[] {
              129, 160, 90, 180, 118, 156, 150, 105, 28, 239, 224, 56, 178, 28, 28, 13, 228, 13, 39,
              248, 54, 29, 211, 136, 131, 213, 31, 115, 162, 196, 191, 178, 123, 184, 122, 30, 240,
              93, 14, 46, 173, 62, 113, 169, 207, 198, 233, 23, 127, 81, 84, 134, 210, 92, 100, 205,
              209, 48, 160, 124, 34, 199, 88, 48, 46, 188, 250, 162, 254, 28, 134, 254, 248, 75,
              247, 171, 42, 185, 189, 236, 7, 244, 80, 111, 57, 224, 30, 83, 173, 76, 192, 188, 145,
              12, 229, 230, 65, 123, 18, 203, 71, 25, 98, 202, 85, 141, 134, 101, 185, 162, 23, 114,
              180, 116, 46, 118, 233, 247, 77, 158, 166, 245, 178, 92, 202, 109, 236, 101, 17, 105,
              174, 167, 241, 165, 238, 145, 133, 25, 171, 89, 185, 154, 10, 52, 77, 8, 28, 222, 110,
              252, 74, 45, 115, 37, 81, 102, 6, 0, 232, 160, 33, 84, 79, 243, 112, 129, 15, 134,
              156, 241, 207, 75, 243, 229, 12, 235, 125, 14, 194, 80, 14, 246, 20, 31, 129, 51, 204,
              83, 135, 138, 133, 48, 10, 56, 100, 74, 22, 196, 203, 249, 150, 65, 125, 44, 254, 58,
              142, 24, 102, 233, 247, 62, 152, 146, 42, 64, 48, 114, 60, 181, 187, 254, 18, 107,
              159, 64, 186, 128, 194, 212, 185, 66, 200, 185, 205, 121, 231, 32, 70, 164, 179, 236,
              158, 147, 70, 152, 120, 14, 36, 25, 163, 135, 22, 61, 152, 47
            }),
        Arguments.of(
            "superlongsupersecretsecret",
            new int[] {
              129, 160, 90, 180, 118, 156, 150, 105, 28, 239, 224, 56, 178, 28, 28, 13, 228, 13, 39,
              248, 54, 29, 211, 136, 131, 213, 31, 115, 162, 196, 191, 178, 123, 184, 122, 30, 240,
              93, 14, 46, 173, 62, 113, 169, 207, 198, 233, 23, 127, 81, 84, 134, 210, 92, 100, 205,
              209, 48, 160, 124, 34, 199, 88, 48, 46, 188, 250, 162, 254, 28, 134, 254, 248, 75,
              247, 171, 42, 185, 189, 236, 7, 244, 80, 111, 57, 224, 30, 83, 173, 76, 192, 188, 145,
              12, 229, 230, 65, 123, 18, 203, 71, 25, 98, 202, 85, 141, 134, 101, 185, 162, 23, 114,
              180, 116, 46, 118, 233, 247, 77, 158, 166, 245, 178, 92, 202, 109, 236, 101, 17, 105,
              174, 167, 241, 165, 238, 145, 133, 25, 171, 89, 185, 154, 10, 52, 77, 8, 28, 222, 110,
              252, 74, 45, 115, 37, 81, 102, 6, 0, 232, 160, 33, 84, 79, 243, 112, 129, 15, 134,
              156, 241, 207, 75, 243, 229, 12, 235, 125, 14, 194, 80, 14, 246, 20, 31, 129, 51, 204,
              83, 135, 138, 133, 48, 10, 56, 100, 74, 22, 196, 203, 249, 150, 65, 125, 44, 254, 58,
              142, 24, 102, 233, 247, 62, 152, 146, 42, 64, 48, 114, 60, 181, 187, 254, 18, 107,
              159, 64, 186, 128, 194, 212, 185, 66, 200, 185, 205, 121, 231, 32, 70, 164, 179, 236,
              158, 147, 70, 152, 120, 14, 36, 25, 163, 135, 22, 61, 152, 47
            }));
  }

  private static int[] toIntArray(byte[] bytes) {
    var ints = new int[bytes.length];
    for (int i = 0; i < bytes.length; i++) ints[i] = bytes[i] & 0xFF;
    return ints;
  }
}
