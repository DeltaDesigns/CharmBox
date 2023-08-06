﻿using Field.General;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Field;

public class DirectXSampler : Tag
{
    public D2Class_SamplerHeader Header;
    public Tag<D3D11_SAMPLER_DESC> Sampler;

    public DirectXSampler(TagHash hash) : base(hash)
    {
    }

    protected override void ParseStructs()
    {
        Header = ReadHeader<D2Class_SamplerHeader>();
        Sampler = PackageHandler.GetTag(typeof(D3D11_SAMPLER_DESC), PackageHandler.GetEntryReference(Hash));
    }

    [StructLayout(LayoutKind.Sequential, Size = 0x34)]
    public struct D3D11_SAMPLER_DESC //Im probably dumb but the enum type isnt being accounted when the data is read so im just gonna manually do the offsets
    {
        public D3D11_FILTER Filter;
        [DestinyOffset(0x04)] 
        public D3D11_TEXTURE_ADDRESS_MODE AddressU;
        [DestinyOffset(0x08)]
        public D3D11_TEXTURE_ADDRESS_MODE AddressV;
        [DestinyOffset(0x0C)]
        public D3D11_TEXTURE_ADDRESS_MODE AddressW;
        [DestinyOffset(0x10)]
        public float MipLODBias;
        public uint MaxAnisotropy;
        public D3D11_COMPARISON_FUNC ComparisonFunc;
        [DestinyOffset(0x1C), MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] BorderColor;
        public float MinLOD;
        public float MaxLOD;
    }

    public enum D3D11_FILTER : int
    {
        MIN_MAG_MIP_POINT = 0,
        MIN_MAG_POINT_MIP_LINEAR = 0x1,
        MIN_POINT_MAG_LINEAR_MIP_POINT = 0x4,
        MIN_POINT_MAG_MIP_LINEAR = 0x5,
        MIN_LINEAR_MAG_MIP_POINT = 0x10,
        MIN_LINEAR_MAG_POINT_MIP_LINEAR = 0x11,
        MIN_MAG_LINEAR_MIP_POINT = 0x14,
        MIN_MAG_MIP_LINEAR = 0x15,
        ANISOTROPIC = 0x55,
        COMPARISON_MIN_MAG_MIP_POINT = 0x80,
        COMPARISON_MIN_MAG_POINT_MIP_LINEAR = 0x81,
        COMPARISON_MIN_POINT_MAG_LINEAR_MIP_POINT = 0x84,
        COMPARISON_MIN_POINT_MAG_MIP_LINEAR = 0x85,
        COMPARISON_MIN_LINEAR_MAG_MIP_POINT = 0x90,
        COMPARISON_MIN_LINEAR_MAG_POINT_MIP_LINEAR = 0x91,
        COMPARISON_MIN_MAG_LINEAR_MIP_POINT = 0x94,
        COMPARISON_MIN_MAG_MIP_LINEAR = 0x95,
        COMPARISON_ANISOTROPIC = 0xd5,
        MINIMUM_MIN_MAG_MIP_POINT = 0x100,
        MINIMUM_MIN_MAG_POINT_MIP_LINEAR = 0x101,
        MINIMUM_MIN_POINT_MAG_LINEAR_MIP_POINT = 0x104,
        MINIMUM_MIN_POINT_MAG_MIP_LINEAR = 0x105,
        MINIMUM_MIN_LINEAR_MAG_MIP_POINT = 0x110,
        MINIMUM_MIN_LINEAR_MAG_POINT_MIP_LINEAR = 0x111,
        MINIMUM_MIN_MAG_LINEAR_MIP_POINT = 0x114,
        MINIMUM_MIN_MAG_MIP_LINEAR = 0x115,
        MINIMUM_ANISOTROPIC = 0x155,
        MAXIMUM_MIN_MAG_MIP_POINT = 0x180,
        MAXIMUM_MIN_MAG_POINT_MIP_LINEAR = 0x181,
        MAXIMUM_MIN_POINT_MAG_LINEAR_MIP_POINT = 0x184,
        MAXIMUM_MIN_POINT_MAG_MIP_LINEAR = 0x185,
        MAXIMUM_MIN_LINEAR_MAG_MIP_POINT = 0x190,
        MAXIMUM_MIN_LINEAR_MAG_POINT_MIP_LINEAR = 0x191,
        MAXIMUM_MIN_MAG_LINEAR_MIP_POINT = 0x194,
        MAXIMUM_MIN_MAG_MIP_LINEAR = 0x195,
        MAXIMUM_ANISOTROPIC = 0x1d5
    }

    public enum D3D11_TEXTURE_ADDRESS_MODE : int
    {
        WRAP = 0x1,
        MIRROR = 0x2,
        CLAMP = 0x3,
        BORDER = 0x4,
        MIRROR_ONCE = 0x5
    }

    public enum D3D11_COMPARISON_FUNC : int
    {
        NEVER = 0x1,
        LESS = 0x2,
        EQUAL = 0x3,
        LESS_EQUAL = 0x4,
        GREATER = 0x5,
        NOT_EQUAL = 0x6,
        GREATER_EQUAL = 0x7,
        ALWAYS = 0x8
    }
}

[StructLayout(LayoutKind.Sequential, Size = 0x08)]
public struct D2Class_SamplerHeader //Header
{
    public ulong Unk00; //Nothing
}
