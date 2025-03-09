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

import java.nio.ByteBuffer;
import java.nio.ByteOrder;
import java.nio.charset.StandardCharsets;

/**
 * Generates machine-specific pseudo keys using a provided secret.
 *
 * <p>The class initializes a machine key from a secret string and can generate pseudo-random bytes
 * based on that key. The pseudo-random bytes are generated using an internal seed derived from the
 * machine key and the {@code AscRandom} generator.
 */
public class MachinePseudoKeys {
  private final byte[] machineKey;

  /**
   * Constructs a new {@code MachinePseudoKeys} instance using the specified secret.
   *
   * <p>The secret is converted to a byte array using UTF-8 encoding and stored as the machine key.
   * If the secret is {@code null} or empty, an {@code IllegalArgumentException} is thrown.
   *
   * @param secret the secret used to generate the machine key; must not be null or empty
   * @throws IllegalArgumentException if the secret is null or empty
   */
  public MachinePseudoKeys(String secret) {
    if (secret != null && !secret.isEmpty()) machineKey = secret.getBytes(StandardCharsets.UTF_8);
    else throw new IllegalArgumentException("Machine key has not been provided");
  }

  /**
   * Returns the machine key constant.
   *
   * <p>This method returns the original machine key as a byte array.
   *
   * @return the machine key as a byte array
   */
  public byte[] getMachineConstant() {
    return machineKey;
  }

  /**
   * Generates a pseudo-random byte array of the specified length using the machine key.
   *
   * <p>The method first creates a combined byte array consisting of a zero-filled array and the
   * machine key. It then converts a portion of this combined array into an integer, which is used
   * as a seed for the {@code AscRandom} pseudorandom generator. Finally, it generates and returns
   * an array of pseudo-random bytes of length {@code bytesCount}.
   *
   * @param bytesCount the number of pseudo-random bytes to generate
   * @return a byte array containing {@code bytesCount} pseudo-random bytes
   */
  public byte[] getMachineConstant(int bytesCount) {
    var zeroBytes = new byte[Integer.BYTES];
    var machineConstant = getMachineConstant();
    var combined = new byte[zeroBytes.length + machineConstant.length];

    System.arraycopy(zeroBytes, 0, combined, 0, zeroBytes.length);
    System.arraycopy(machineConstant, 0, combined, zeroBytes.length, machineConstant.length);

    var icnst = bytesToInt(combined, combined.length - Integer.BYTES);

    var rnd = new AscRandom(icnst);

    var buff = new byte[bytesCount];
    rnd.nextBytes(buff);

    return buff;
  }

  /**
   * Converts a {@code double} value to an 8-byte array in little-endian order.
   *
   * <p>This utility method allocates a byte buffer of size 8, sets it to little-endian order, and
   * writes the double value into it.
   *
   * @param value the double value to convert
   * @return an 8-byte array representing the {@code double} value in little-endian order
   */
  private static byte[] doubleToBytes(double value) {
    var buffer = ByteBuffer.allocate(8).order(ByteOrder.LITTLE_ENDIAN);
    buffer.putDouble(value);
    return buffer.array();
  }

  /**
   * Converts four bytes from a byte array starting at the given offset into an integer.
   *
   * <p>The conversion uses little-endian byte order.
   *
   * @param bytes the byte array containing the bytes to convert
   * @param offset the starting offset in the byte array
   * @return the integer value represented by the four bytes starting at {@code offset}
   */
  private static int bytesToInt(byte[] bytes, int offset) {
    var buffer = ByteBuffer.wrap(bytes, offset, Integer.BYTES).order(ByteOrder.LITTLE_ENDIAN);
    return buffer.getInt();
  }
}
